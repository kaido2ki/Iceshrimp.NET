using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityDeliverService(
	ILogger<ActivityDeliverService> logger,
	DatabaseContext db,
	QueueService queueService
) {
	public async Task DeliverToFollowersAsync(ASActivity activity, User actor, IEnumerable<User> recipients) {
		logger.LogDebug("Delivering activity {id} to followers", activity.Id);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		var inboxUrls = await db.Followings.Where(p => p.Followee == actor)
		                        .Select(p => p.FollowerSharedInbox ?? p.FollowerInbox)
		                        .Where(p => p != null)
		                        .Select(p => p!)
		                        .Distinct()
		                        .ToListAsync();

		// We want to deliver activities to the explicitly specified recipients first
		inboxUrls = recipients.Select(p => p.SharedInbox ?? p.Inbox).Where(p => p != null).Select(p => p!)
		                      .Concat(inboxUrls)
		                      .Distinct()
		                      .ToList();

		if (inboxUrls.Count == 0) return;

		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == actor);
		var payload = await activity.SignAndCompactAsync(keypair);

		foreach (var inboxUrl in inboxUrls)
			await queueService.DeliverQueue.EnqueueAsync(new DeliverJob {
				InboxUrl    = inboxUrl,
				Payload     = payload,
				ContentType = "application/activity+json",
				UserId      = actor.Id
			});
	}

	public async Task DeliverToAsync(ASActivity activity, User actor, IEnumerable<User> recipients) {
		foreach (var recipient in recipients.Where(p => p.Host != null))
			await DeliverToAsync(activity, actor, recipient);
	}

	public async Task DeliverToAsync(ASActivity activity, User actor, User recipient) {
		var inboxUrl = recipient.Inbox ?? recipient.SharedInbox;
		if (recipient.Host == null || inboxUrl == null)
			throw new GracefulException("Refusing to deliver to local user");

		logger.LogDebug("Delivering activity {id} to {recipient}", activity.Id, inboxUrl);
		if (activity.Actor == null) throw new Exception("Actor must not be null");

		var keypair = await db.UserKeypairs.FirstAsync(p => p.User == actor);
		var payload = await activity.SignAndCompactAsync(keypair);

		await queueService.DeliverQueue.EnqueueAsync(new DeliverJob {
			InboxUrl    = inboxUrl,
			Payload     = payload,
			ContentType = "application/activity+json",
			UserId      = actor.Id
		});
	}
}