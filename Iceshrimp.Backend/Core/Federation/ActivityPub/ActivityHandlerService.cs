using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter",
                 Justification = "We want to enforce AS types, so we can't use the base type here")]
public class ActivityHandlerService(
	ILogger<ActivityHandlerService> logger,
	NoteService noteSvc,
	UserService userSvc,
	UserResolver userResolver,
	DatabaseContext db,
	QueueService queueService,
	ActivityRenderer activityRenderer,
	IOptions<Config.InstanceSection> config,
	IOptions<Config.SecuritySection> security,
	FederationControlService federationCtrl,
	ObjectResolver resolver,
	NotificationService notificationSvc,
	ActivityDeliverService deliverSvc
) {
	public async Task PerformActivityAsync(ASActivity activity, string? inboxUserId, string? authFetchUserId) {
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot perform activity as actor 'null'");
		if (await federationCtrl.ShouldBlockAsync(activity.Actor.Id))
			throw GracefulException.UnprocessableEntity("Instance is blocked");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Activity object is null");

		var resolvedActor = await userResolver.ResolveAsync(activity.Actor.Id);

		if (security.Value.AuthorizedFetch && authFetchUserId == null)
			throw GracefulException
				.UnprocessableEntity("Refusing to process activity without authFetchUserId in authorized fetch mode");
		if (resolvedActor.Id != authFetchUserId && authFetchUserId != null)
			throw GracefulException
				.UnprocessableEntity($"Authorized fetch user id {authFetchUserId} doesn't match resolved actor id {resolvedActor.Id}");

		// Resolve object & children
		activity.Object = await resolver.ResolveObject(activity.Object) ??
		                  throw GracefulException.UnprocessableEntity("Failed to resolve activity object");

		//TODO: validate inboxUserId

		switch (activity) {
			case ASCreate: {
				//TODO: should we handle other types of creates?
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Create activity object is invalid");
				await noteSvc.ProcessNoteAsync(note, activity.Actor);
				return;
			}
			case ASDelete: {
				if (activity.Object is not ASTombstone tombstone)
					throw GracefulException.UnprocessableEntity("Delete activity object is invalid");
				if (await db.Notes.AnyAsync(p => p.Uri == tombstone.Id)) {
					await noteSvc.DeleteNoteAsync(tombstone, activity.Actor);
					return;
				}

				if (await db.Users.AnyAsync(p => p.Uri == tombstone.Id)) {
					if (tombstone.Id != activity.Actor.Id)
						throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

					//TODO: handle user deletes
					throw new NotImplementedException("User deletes aren't supported yet");
				}

				throw GracefulException.UnprocessableEntity("Delete activity object is unknown or invalid");
			}
			case ASFollow: {
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
				await FollowAsync(obj, activity.Actor, activity.Id);
				return;
			}
			case ASUnfollow: {
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
				await UnfollowAsync(obj, activity.Actor);
				return;
			}
			case ASAccept: {
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Accept activity object is invalid");
				await AcceptAsync(obj, activity.Actor);
				return;
			}
			case ASReject: {
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Reject activity object is invalid");
				await RejectAsync(obj, activity.Actor);
				return;
			}
			case ASUndo: {
				switch (activity.Object) {
					case ASFollow { Object: ASActor followee }:
						await UnfollowAsync(followee, activity.Actor);
						return;
					case ASLike { Object: ASNote likedNote }:
						await noteSvc.UnlikeNoteAsync(likedNote, activity.Actor);
						return;
					default:
						throw GracefulException.UnprocessableEntity("Undo activity object is invalid");
				}
			}
			case ASLike: {
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Like activity object is invalid");
				await noteSvc.LikeNoteAsync(note, activity.Actor);
				return;
			}
			case ASUpdate: {
				switch (activity.Object) {
					case ASActor actor:
						if (actor.Id != activity.Actor.Id)
							throw GracefulException.UnprocessableEntity("Refusing to update actor with mismatching id");
						await userSvc.UpdateUserAsync(resolvedActor, actor);
						return;
					case ASNote note:
						await noteSvc.ProcessNoteUpdateAsync(note, activity.Actor, resolvedActor);
						return;
					default:
						throw GracefulException.UnprocessableEntity("Update activity object is invalid");
				}
			}
			default: {
				throw new NotImplementedException($"Activity type {activity.Type} is unknown");
			}
		}
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Projectable functions can very much be translated to SQL")]
	private async Task FollowAsync(ASActor followeeActor, ASActor followerActor, string requestId) {
		var follower = await userResolver.ResolveAsync(followerActor.Id);
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		if (followee.Host != null) throw new Exception("Cannot process follow for remote followee");

		// Check blocks first
		if (await db.Users.AnyAsync(p => p == followee && p.IsBlocking(follower))) {
			var activity = activityRenderer.RenderReject(followee, follower, requestId);
			await deliverSvc.DeliverToAsync(activity, followee, follower);
			return;
		}

		if (followee.IsLocked) {
			var followRequest = new FollowRequest {
				Id                  = IdHelpers.GenerateSlowflakeId(),
				CreatedAt           = DateTime.UtcNow,
				Followee            = followee,
				Follower            = follower,
				FolloweeHost        = followee.Host,
				FollowerHost        = follower.Host,
				FolloweeInbox       = followee.Inbox,
				FollowerInbox       = follower.Inbox,
				FolloweeSharedInbox = followee.SharedInbox,
				FollowerSharedInbox = follower.SharedInbox,
				RequestId           = requestId
			};

			await db.AddAsync(followRequest);
			await db.SaveChangesAsync();
			await notificationSvc.GenerateFollowRequestReceivedNotification(followRequest);
			return;
		}

		var acceptActivity = activityRenderer.RenderAccept(followeeActor,
		                                                   ActivityRenderer.RenderFollow(followerActor,
				                                                    followeeActor, requestId));
		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == followee);
		var payload = await acceptActivity.SignAndCompactAsync(keypair);
		var inboxUri = follower.SharedInbox ??
		               follower.Inbox ?? throw new Exception("Can't accept follow: user has no inbox");
		var job = new DeliverJob {
			InboxUrl      = inboxUri,
			RecipientHost = follower.Host ?? throw new Exception("Can't accept follow: follower host is null"),
			Payload       = payload,
			ContentType   = "application/activity+json",
			UserId        = followee.Id
		};
		await queueService.DeliverQueue.EnqueueAsync(job);

		if (!await db.Followings.AnyAsync(p => p.Follower == follower && p.Followee == followee)) {
			var following = new Following {
				Id                  = IdHelpers.GenerateSlowflakeId(),
				CreatedAt           = DateTime.UtcNow,
				Followee            = followee,
				Follower            = follower,
				FolloweeHost        = followee.Host,
				FollowerHost        = follower.Host,
				FolloweeInbox       = followee.Inbox,
				FollowerInbox       = follower.Inbox,
				FolloweeSharedInbox = followee.SharedInbox,
				FollowerSharedInbox = follower.SharedInbox
			};

			follower.FollowingCount++;
			followee.FollowersCount++;

			await db.AddAsync(following);
			await db.SaveChangesAsync();
			await notificationSvc.GenerateFollowNotification(follower, followee);
		}
	}

	private async Task UnfollowAsync(ASActor followeeActor, ASActor followerActor) {
		//TODO: send reject? or do we not want to copy that part of the old ap core
		var follower = await userResolver.ResolveAsync(followerActor.Id);
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();

		// We don't want to use ExecuteDelete for this one to ensure consistency with following counters
		var followings = await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ToListAsync();
		if (followings.Count > 0) {
			followee.FollowersCount -= followings.Count;
			follower.FollowingCount -= followings.Count;
			db.RemoveRange(followings);
			await db.SaveChangesAsync();

			if (followee.Host != null) return;
			await db.Notifications
			        .Where(p => p.Type == Notification.NotificationType.Follow &&
			                    p.Notifiee == followee &&
			                    p.Notifier == follower)
			        .ExecuteDeleteAsync();
		}
	}

	private async Task AcceptAsync(ASFollow obj, ASActor actor) {
		var prefix = $"https://{config.Value.WebDomain}/follows/";
		if (!obj.Id.StartsWith(prefix))
			throw GracefulException.UnprocessableEntity($"Object id '{obj.Id}' not a valid follow request id");

		var resolvedActor = await userResolver.ResolveAsync(actor.Id);
		var ids           = obj.Id[prefix.Length..].TrimEnd('/').Split("/");
		if (ids.Length != 2 || ids[1] != resolvedActor.Id)
			throw GracefulException
				.UnprocessableEntity($"Actor id '{resolvedActor.Id}' doesn't match followee id '{ids[1]}'");

		var request = await db.FollowRequests
		                      .Include(p => p.Follower.UserProfile)
		                      .Include(p => p.Followee.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Followee == resolvedActor && p.FollowerId == ids[0]);

		if (request == null)
			throw GracefulException
				.UnprocessableEntity($"No follow request matching follower '{ids[0]}' and followee '{resolvedActor.Id}' found");

		var following = new Following {
			Id                  = IdHelpers.GenerateSlowflakeId(),
			CreatedAt           = DateTime.UtcNow,
			Follower            = request.Follower,
			Followee            = resolvedActor,
			FollowerHost        = request.FollowerHost,
			FolloweeHost        = request.FolloweeHost,
			FollowerInbox       = request.FollowerInbox,
			FolloweeInbox       = request.FolloweeInbox,
			FollowerSharedInbox = request.FollowerSharedInbox,
			FolloweeSharedInbox = request.FolloweeSharedInbox
		};

		resolvedActor.FollowersCount++;
		request.Follower.FollowingCount++;

		db.Remove(request);
		await db.AddAsync(following);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateFollowRequestAcceptedNotification(request);
	}

	private async Task RejectAsync(ASFollow follow, ASActor actor) {
		if (follow is not { Actor: not null })
			throw GracefulException.UnprocessableEntity("Refusing to reject object with invalid follow object");

		var resolvedActor    = await userResolver.ResolveAsync(actor.Id);
		var resolvedFollower = await userResolver.ResolveAsync(follow.Actor.Id);
		if (resolvedFollower is not { Host: null })
			throw GracefulException.UnprocessableEntity("Refusing to reject remote follow");

		await db.FollowRequests.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
		var count = await db.Followings.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		                    .ExecuteDeleteAsync();
		if (count > 0) {
			resolvedActor.FollowersCount    -= count;
			resolvedFollower.FollowingCount -= count;
			await db.SaveChangesAsync();
		}

		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.FollowRequestAccepted)
		        .Where(p => p.Notifiee == resolvedFollower &&
		                    p.Notifier == resolvedActor)
		        .ExecuteDeleteAsync();
	}
}