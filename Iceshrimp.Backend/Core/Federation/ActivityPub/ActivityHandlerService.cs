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
	ActivityDeliverService deliverSvc,
	ObjectResolver objectResolver,
	FollowupTaskService followupTaskSvc
)
{
	public async Task PerformActivityAsync(ASActivity activity, string? inboxUserId, string? authFetchUserId)
	{
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null)
			throw GracefulException.UnprocessableEntity("Cannot perform activity as actor 'null'");
		if (await federationCtrl.ShouldBlockAsync(activity.Actor.Id))
			throw GracefulException.UnprocessableEntity("Instance is blocked");
		if (activity.Object == null && activity is not ASBite)
			throw GracefulException.UnprocessableEntity("Activity object is null");

		var resolvedActor = await userResolver.ResolveAsync(activity.Actor.Id);
		if (security.Value.AuthorizedFetch && authFetchUserId == null)
			throw GracefulException
				.UnprocessableEntity("Refusing to process activity without authFetchUserId in authorized fetch mode");
		if (resolvedActor.Id != authFetchUserId && authFetchUserId != null)
			throw GracefulException
				.UnprocessableEntity($"Authorized fetch user id {authFetchUserId} doesn't match resolved actor id {resolvedActor.Id}");
		if (resolvedActor.Host == null || resolvedActor.Uri == null)
			throw new Exception("resolvedActor.Host and resolvedActor.Uri must not be null at this stage");

		UpdateInstanceMetadataInBackground(resolvedActor.Host, new Uri(resolvedActor.Uri).Host);

		// Resolve object & children
		if (activity.Object != null)
			activity.Object = await resolver.ResolveObject(activity.Object) ??
			                  throw GracefulException.UnprocessableEntity("Failed to resolve activity object");

		//TODO: validate inboxUserId

		switch (activity)
		{
			case ASCreate:
			{
				//TODO: should we handle other types of creates?
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Create activity object is invalid");
				await noteSvc.ProcessNoteAsync(note, resolvedActor);
				return;
			}
			case ASDelete:
			{
				if (activity.Object is ASActor actor)
				{
					if (!await db.Users.AnyAsync(p => p.Uri == actor.Id))
						return;

					if (activity.Actor.Id != actor.Id)
						throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

					await userSvc.DeleteUserAsync(actor);
					return;
				}

				if (activity.Object is not ASTombstone tombstone)
					throw GracefulException.UnprocessableEntity("Delete activity object is invalid");
				if (await db.Notes.AnyAsync(p => p.Uri == tombstone.Id))
				{
					await noteSvc.DeleteNoteAsync(tombstone, resolvedActor);
					return;
				}

				if (await db.Users.AnyAsync(p => p.Uri == tombstone.Id))
				{
					if (tombstone.Id != activity.Actor.Id)
						throw GracefulException.UnprocessableEntity("Refusing to delete user: actor doesn't match");

					await userSvc.DeleteUserAsync(activity.Actor);
				}

				logger.LogDebug("Delete activity object {id} is unknown, skipping", tombstone.Id);
				return;
			}
			case ASFollow:
			{
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
				await FollowAsync(obj, activity.Actor, resolvedActor, activity.Id);
				return;
			}
			case ASUnfollow:
			{
				if (activity.Object is not ASActor obj)
					throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
				await UnfollowAsync(obj, resolvedActor);
				return;
			}
			case ASAccept:
			{
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Accept activity object is invalid");
				await AcceptAsync(obj, resolvedActor);
				return;
			}
			case ASReject:
			{
				if (activity.Object is not ASFollow obj)
					throw GracefulException.UnprocessableEntity("Reject activity object is invalid");
				await RejectAsync(obj, resolvedActor);
				return;
			}
			case ASUndo:
			{
				switch (activity.Object)
				{
					case ASFollow { Object: ASActor followee }:
						await UnfollowAsync(followee, resolvedActor);
						return;
					case ASLike { Object: ASNote likedNote }:
						await noteSvc.UnlikeNoteAsync(likedNote, resolvedActor);
						return;
					case ASAnnounce { Object: ASNote likedNote }:
						await noteSvc.UndoAnnounceAsync(likedNote, resolvedActor);
						return;
					default:
						throw GracefulException.UnprocessableEntity("Undo activity object is invalid");
				}
			}
			case ASLike:
			{
				if (activity.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Like activity object is invalid");
				await noteSvc.LikeNoteAsync(note, resolvedActor);
				return;
			}
			case ASUpdate:
			{
				switch (activity.Object)
				{
					case ASActor actor:
						if (actor.Id != activity.Actor.Id)
							throw GracefulException.UnprocessableEntity("Refusing to update actor with mismatching id");
						await userSvc.UpdateUserAsync(resolvedActor, actor);
						return;
					case ASNote note:
						await noteSvc.ProcessNoteUpdateAsync(note, resolvedActor);
						return;
					default:
						throw GracefulException.UnprocessableEntity("Update activity object is invalid");
				}
			}
			case ASBite bite:
			{
				var target = await objectResolver.ResolveObject(bite.Target);
				var dbBite = target switch
				{
					ASActor targetActor => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetUser = await userResolver.ResolveAsync(targetActor.Id)
					},
					ASNote targetNote => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetNote = await noteSvc.ResolveNoteAsync(targetNote.Id)
					},
					ASBite targetBite => new Bite
					{
						Id        = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt = bite.PublishedAt ?? DateTime.UtcNow,
						Uri       = bite.Id,
						User      = resolvedActor,
						UserHost  = resolvedActor.Host,
						TargetBite =
							await db.Bites.FirstAsync(p => p.UserHost == null &&
							                               p.Id == Bite.GetIdFromPublicUri(targetBite.Id, config.Value))
					},
					null => throw
						GracefulException.UnprocessableEntity($"Failed to resolve bite target {bite.Target.Id}"),
					_ when bite.To?.Id != null => new Bite
					{
						Id         = IdHelpers.GenerateSlowflakeId(bite.PublishedAt),
						CreatedAt  = bite.PublishedAt ?? DateTime.UtcNow,
						Uri        = bite.Id,
						User       = resolvedActor,
						UserHost   = resolvedActor.Host,
						TargetUser = await userResolver.ResolveAsync(bite.To.Id)
					},
					_ => throw GracefulException
						.UnprocessableEntity($"Invalid bite target {target.Id} with type {target.Type}")

					//TODO: more fallback
				};

				if (dbBite.TargetUser?.Host != null ||
				    dbBite.TargetNote?.User.Host != null ||
				    dbBite.TargetBite?.User.Host != null)
					throw GracefulException.Accepted("Ignoring bite for remote user");

				await db.AddAsync(dbBite);
				await db.SaveChangesAsync();
				await notificationSvc.GenerateBiteNotification(dbBite);
				return;
			}
			case ASAnnounce announce:
			{
				if (announce.Object is not ASNote note)
					throw GracefulException.UnprocessableEntity("Invalid or unsupported announce object");

				var dbNote = await noteSvc.ResolveNoteAsync(note.Id, note.VerifiedFetch ? note : null);
				await noteSvc.CreateNoteAsync(resolvedActor, announce.GetVisibility(activity.Actor), renote: dbNote);
				return;
			}
			default:
				throw new NotImplementedException($"Activity type {activity.Type} is unknown");
		}
	}

	private void UpdateInstanceMetadataInBackground(string host, string webDomain)
	{
		_ = followupTaskSvc.ExecuteTask("UpdateInstanceMetadata", async provider =>
		{
			var instanceSvc = provider.GetRequiredService<InstanceService>();
			await instanceSvc.UpdateInstanceStatusAsync(host, webDomain);
		});
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Projectable functions can very much be translated to SQL")]
	private async Task FollowAsync(ASActor followeeActor, ASActor followerActor, User follower, string requestId)
	{
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		if (followee.Host != null) throw new Exception("Cannot process follow for remote followee");

		// Check blocks first
		if (await db.Users.AnyAsync(p => p == followee && p.IsBlocking(follower)))
		{
			var activity = activityRenderer.RenderReject(followee, follower, requestId);
			await deliverSvc.DeliverToAsync(activity, followee, follower);
			return;
		}

		if (followee.IsLocked)
		{
			var followRequest = new FollowRequest
			{
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
		var job = new DeliverJob
		{
			InboxUrl      = inboxUri,
			RecipientHost = follower.Host ?? throw new Exception("Can't accept follow: follower host is null"),
			Payload       = payload,
			ContentType   = "application/activity+json",
			UserId        = followee.Id
		};
		await queueService.DeliverQueue.EnqueueAsync(job);

		if (!await db.Followings.AnyAsync(p => p.Follower == follower && p.Followee == followee))
		{
			var following = new Following
			{
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

	private async Task UnfollowAsync(ASActor followeeActor, User follower)
	{
		//TODO: send reject? or do we not want to copy that part of the old ap core
		var followee = await userResolver.ResolveAsync(followeeActor.Id);

		await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();

		// We don't want to use ExecuteDelete for this one to ensure consistency with following counters
		var followings = await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ToListAsync();
		if (followings.Count > 0)
		{
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

	private async Task AcceptAsync(ASFollow obj, User actor)
	{
		var prefix = $"https://{config.Value.WebDomain}/follows/";
		if (!obj.Id.StartsWith(prefix))
			throw GracefulException.UnprocessableEntity($"Object id '{obj.Id}' not a valid follow request id");

		var ids = obj.Id[prefix.Length..].TrimEnd('/').Split("/");
		if (ids.Length != 2 || ids[1] != actor.Id)
			throw GracefulException
				.UnprocessableEntity($"Actor id '{actor.Id}' doesn't match followee id '{ids[1]}'");

		var request = await db.FollowRequests
		                      .Include(p => p.Follower.UserProfile)
		                      .Include(p => p.Followee.UserProfile)
		                      .FirstOrDefaultAsync(p => p.Followee == actor && p.FollowerId == ids[0]);

		if (request == null)
			throw GracefulException
				.UnprocessableEntity($"No follow request matching follower '{ids[0]}' and followee '{actor.Id}' found");

		var following = new Following
		{
			Id                  = IdHelpers.GenerateSlowflakeId(),
			CreatedAt           = DateTime.UtcNow,
			Follower            = request.Follower,
			Followee            = actor,
			FollowerHost        = request.FollowerHost,
			FolloweeHost        = request.FolloweeHost,
			FollowerInbox       = request.FollowerInbox,
			FolloweeInbox       = request.FolloweeInbox,
			FollowerSharedInbox = request.FollowerSharedInbox,
			FolloweeSharedInbox = request.FolloweeSharedInbox
		};

		actor.FollowersCount++;
		request.Follower.FollowingCount++;

		db.Remove(request);
		await db.AddAsync(following);
		await db.SaveChangesAsync();
		await notificationSvc.GenerateFollowRequestAcceptedNotification(request);
	}

	private async Task RejectAsync(ASFollow follow, User actor)
	{
		if (follow is not { Actor: not null })
			throw GracefulException.UnprocessableEntity("Refusing to reject object with invalid follow object");

		var resolvedFollower = await userResolver.ResolveAsync(follow.Actor.Id);
		if (resolvedFollower is not { Host: null })
			throw GracefulException.UnprocessableEntity("Refusing to reject remote follow");

		await db.FollowRequests.Where(p => p.Followee == actor && p.Follower == resolvedFollower)
		        .ExecuteDeleteAsync();
		var count = await db.Followings.Where(p => p.Followee == actor && p.Follower == resolvedFollower)
		                    .ExecuteDeleteAsync();
		if (count > 0)
		{
			actor.FollowersCount            -= count;
			resolvedFollower.FollowingCount -= count;
			await db.SaveChangesAsync();
		}

		await db.Notifications
		        .Where(p => p.Type == Notification.NotificationType.FollowRequestAccepted)
		        .Where(p => p.Notifiee == resolvedFollower &&
		                    p.Notifier == actor)
		        .ExecuteDeleteAsync();

		await db.UserListMembers
		        .Where(p => p.UserList.User == resolvedFollower && p.User == actor)
		        .ExecuteDeleteAsync();
	}
}