using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityDeliverService(
	ILogger<ActivityDeliverService> logger,
	DatabaseContext db,
	HttpRequestService httpRqSvc,
	QueueService queueService
) {
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
}