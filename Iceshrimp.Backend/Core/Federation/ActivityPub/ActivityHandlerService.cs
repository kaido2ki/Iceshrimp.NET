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

public class ActivityHandlerService(
	ILogger<ActivityHandlerService> logger,
	NoteService noteSvc,
	UserResolver userResolver,
	DatabaseContext db,
	QueueService queueService,
	ActivityRenderer activityRenderer,
	IOptions<Config.InstanceSection> config,
	FederationControlService federationCtrl,
	ObjectResolver resolver
) {
	public async Task PerformActivityAsync(ASActivity activity, string? inboxUserId) {
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot perform activity as actor 'null'");
		if (await federationCtrl.ShouldBlockAsync(activity.Actor.Id))
			throw GracefulException.UnprocessableEntity("Instance is blocked");
		if (activity.Object == null)
			throw GracefulException.UnprocessableEntity("Activity object is null");
		if (activity.Object.IsUnresolved)
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
				if (activity.Object is not { } obj)
					throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
				await FollowAsync(obj, activity.Actor, activity.Id);
				return;
			}
			case ASUnfollow: {
				if (activity.Object is not { } obj)
					throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
				await UnfollowAsync(obj, activity.Actor);
				return;
			}
			case ASAccept: {
				if (activity.Object is not { } obj)
					throw GracefulException.UnprocessableEntity("Accept activity object is invalid");
				await AcceptAsync(obj, activity.Actor);
				return;
			}
			case ASReject: {
				if (activity.Object is not { } obj)
					throw GracefulException.UnprocessableEntity("Reject activity object is invalid");
				await RejectAsync(obj, activity.Actor);
				return;
			}
			case ASUndo: {
				//TODO: what other types of undo objects are there?
				if (activity.Object is not ASActivity { Type: ASActivity.Types.Follow, Object: not null } undoActivity)
					throw new NotImplementedException("Undo activity object is invalid");
				await UnfollowAsync(undoActivity.Object, activity.Actor);
				return;
			}
			default: {
				throw new NotImplementedException($"Activity type {activity.Type} is unknown");
			}
		}
	}

	private async Task FollowAsync(ASObject followeeActor, ASObject followerActor, string requestId) {
		var follower = await userResolver.ResolveAsync(followerActor.Id);
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		if (followee.Host != null) throw new Exception("Cannot process follow for remote followee");

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
			InboxUrl    = inboxUri,
			Payload     = payload,
			ContentType = "application/activity+json",
			UserId      = followee.Id
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

			await db.AddAsync(following);
			await db.SaveChangesAsync();
		}
	}

	private async Task UnfollowAsync(ASObject followeeActor, ASObject followerActor) {
		//TODO: send reject? or do we not want to copy that part of the old ap core
		var follower = await userResolver.ResolveAsync(followerActor.Id);
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
		await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
	}

	private async Task AcceptAsync(ASObject obj, ASObject actor) {
		var prefix = $"https://{config.Value.WebDomain}/follows/";
		if (!obj.Id.StartsWith(prefix))
			throw GracefulException.UnprocessableEntity($"Object id '{obj.Id}' not a valid follow request id");

		var resolvedActor = await userResolver.ResolveAsync(actor.Id);
		var ids           = obj.Id[prefix.Length..].TrimEnd('/').Split("/");
		if (ids.Length != 2 || ids[1] != resolvedActor.Id)
			throw GracefulException
				.UnprocessableEntity($"Actor id '{resolvedActor.Id}' doesn't match followee id '{ids[1]}'");

		var request = await db.FollowRequests
		                      .Include(p => p.Follower)
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
		db.Remove(request);
		await db.AddAsync(following);
		await db.SaveChangesAsync();
	}

	private async Task RejectAsync(ASObject obj, ASObject actor) {
		if (obj is not ASFollow { Actor: not null } follow)
			throw GracefulException.UnprocessableEntity("Refusing to reject object with invalid follow object");

		var resolvedActor    = await userResolver.ResolveAsync(actor.Id);
		var resolvedFollower = await userResolver.ResolveAsync(follow.Actor.Id);
		if (resolvedFollower is not { Host: null })
			throw GracefulException.UnprocessableEntity("Refusing to reject remote follow");

		await db.FollowRequests.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
		await db.Followings.Where(p => p.Followee == resolvedActor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
	}
}