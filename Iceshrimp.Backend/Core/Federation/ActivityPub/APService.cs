using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class APService(
	ILogger<APService> logger,
	NoteService noteSvc,
	UserResolver userResolver,
	DatabaseContext db
) {
	public Task PerformActivity(ASActivity activity, string? inboxUserId) {
		logger.LogDebug("Processing activity: {activity}", activity.Id);
		if (activity.Actor == null) throw new Exception("Cannot perform activity as actor 'null'");

		//TODO: validate inboxUserId

		switch (activity.Type) {
			case "https://www.w3.org/ns/activitystreams#Create": {
				if (activity.Object is ASNote note) return noteSvc.CreateNote(note, activity.Actor);
				throw new NotImplementedException();
			}
			case "https://www.w3.org/ns/activitystreams#Like": {
				throw new NotImplementedException();
			}
			case "https://www.w3.org/ns/activitystreams#Follow": {
				if (activity.Object is { } obj) return Follow(obj, activity.Actor);
				throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
			}
			case "https://www.w3.org/ns/activitystreams#Unfollow": {
				if (activity.Object is { } obj) return Unfollow(obj, activity.Actor);
				throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
			}
			default: {
				throw new NotImplementedException();
			}
		}
	}

	private async Task Follow(ASObject followeeActor, ASObject followerActor) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

		if (await db.Followings.AnyAsync(p => p.Follower == follower && p.Followee == followee)) {
			logger.LogDebug("Ignoring follow, relationship already exists");
			return;
		}
		//TODO: also check (or handle at all) follow requests

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

	private async Task Unfollow(ASObject followeeActor, ASObject followerActor) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

		await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
		//TODO: also check (or handle at all) follow requests
	}
}