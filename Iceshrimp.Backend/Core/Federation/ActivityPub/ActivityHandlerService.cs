using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityHandlerService(
	ILogger<ActivityHandlerService> logger,
	NoteService noteSvc,
	UserResolver userResolver,
	DatabaseContext db,
	HttpRequestService httpRqSvc,
	QueueService queueService,
	ActivityRenderer activityRenderer
) {
	public Task PerformActivity(ASActivity activity, string? inboxUserId) {
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null) throw new Exception("Cannot perform activity as actor 'null'");

		//TODO: validate inboxUserId

		switch (activity.Type) {
			case ASActivity.Types.Create: {
				//TODO: implement the rest
				if (activity.Object is ASNote note) return noteSvc.ProcessNote(note, activity.Actor);
				throw GracefulException.UnprocessableEntity("Create activity object is invalid");
			}
			case ASActivity.Types.Follow: {
				if (activity.Object is { } obj) return Follow(obj, activity.Actor, activity.Id);
				throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
			}
			case ASActivity.Types.Unfollow: {
				if (activity.Object is { } obj) return Unfollow(obj, activity.Actor);
				throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
			}
			case ASActivity.Types.Undo: {
				//TODO: implement the rest
				//TODO: test if this actually works
				if (activity.Object is ASActivity { Type: ASActivity.Types.Follow, Object: not null } undoActivity)
					return Unfollow(undoActivity.Object, activity.Actor);
				throw new NotImplementedException("Unsupported undo operation");
			}
			default: {
				throw new NotImplementedException($"Activity type {activity.Type} is unknown");
			}
		}
	}

	private async Task Follow(ASObject followeeActor, ASObject followerActor, string requestId) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

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
		                                                   activityRenderer.RenderFollow(followerActor,
				                                                    followeeActor, requestId));
		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == followee);
		var payload = await acceptActivity.SignAndCompact(keypair);
		var inboxUri = follower.SharedInbox ??
		               follower.Inbox ?? throw new Exception("Can't accept follow: user has no inbox");
		var request = await httpRqSvc.PostSigned(inboxUri, payload, "application/activity+json", followee, keypair);
		var job     = new DeliverJob(request);
		queueService.DeliverQueue.Enqueue(job);

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

	private async Task Unfollow(ASObject followeeActor, ASObject followerActor) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

		await db.FollowRequests.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
		await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
	}
}