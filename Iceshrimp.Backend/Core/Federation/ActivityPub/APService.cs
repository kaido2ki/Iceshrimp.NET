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

public class APService(
	IOptions<Config.InstanceSection> config,
	ILogger<APService> logger,
	NoteService noteSvc,
	UserResolver userResolver,
	DatabaseContext db,
	HttpRequestService httpRqSvc,
	QueueService queueService
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
				if (activity.Object is { } obj) return Follow(obj, activity.Actor, activity.Id);
				throw GracefulException.UnprocessableEntity("Follow activity object is invalid");
			}
			case "https://www.w3.org/ns/activitystreams#Unfollow": {
				if (activity.Object is { } obj) return Unfollow(obj, activity.Actor, activity.Id);
				throw GracefulException.UnprocessableEntity("Unfollow activity object is invalid");
			}
			default: {
				throw new NotImplementedException();
			}
		}
	}

	public async Task DeliverToFollowers(ASActivity activity, User actor) {
		logger.LogDebug("Delivering activity {id} to followers", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		var inboxUrls = await db.Followings.Where(p => p.Followee == actor)
		                        .Select(p => p.FollowerSharedInbox ?? p.FollowerInbox)
		                        .Where(p => p != null)
		                        .Select(p => p!)
		                        .Distinct()
		                        .ToListAsync();

		if (inboxUrls.Count == 0) return;

		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == actor);
		var payload = await activity.SignAndCompact(keypair);

		foreach (var inboxUrl in inboxUrls) {
			var request = await httpRqSvc.PostSigned(inboxUrl, payload, "application/activity+json", actor, keypair);
			queueService.DeliverQueue.Enqueue(new DeliverJob(request));
		}
	}

	private async Task Follow(ASObject followeeActor, ASObject followerActor, string requestId) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

		if (followee.Host != null) throw new Exception("Cannot process follow for remote followee");

		//TODO: handle follow requests

		var acceptActivity = new ASActivity {
			Id   = $"https://{config.Value.WebDomain}/activities/{new Guid().ToString().ToLowerInvariant()}",
			Type = "https://www.w3.org/ns/activitystreams#Accept",
			Actor = new ASActor {
				Id = followeeActor.Id
			},
			Object = new ASObject {
				Id = requestId
			}
		};

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

	private async Task Unfollow(ASObject followeeActor, ASObject followerActor, string id) {
		var follower = await userResolver.Resolve(followerActor.Id);
		var followee = await userResolver.Resolve(followeeActor.Id);

		await db.Followings.Where(p => p.Follower == follower && p.Followee == followee).ExecuteDeleteAsync();
		//TODO: also check (or handle at all) follow requests
	}
}