using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using ProtoBuf;
using StackExchange.Redis;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Queues;

public class PreDeliverQueue {
	public static JobQueue<PreDeliverJob> Create(IConnectionMultiplexer redis, string prefix) {
		return new JobQueue<PreDeliverJob>("pre-deliver", PreDeliverQueueProcessorDelegateAsync, 4, redis, prefix);
	}

	private static async Task PreDeliverQueueProcessorDelegateAsync(
		PreDeliverJob job, IServiceProvider scope, CancellationToken token
	) {
		var logger   = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var db       = scope.GetRequiredService<DatabaseContext>();
		var queueSvc = scope.GetRequiredService<QueueService>();

		var parsed       = JToken.Parse(job.SerializedActivity);
		var expanded     = LdHelpers.Expand(parsed) ?? throw new Exception("Failed to expand activity");
		var deserialized = ASObject.Deserialize(expanded);
		if (deserialized is not ASActivity activity)
			throw new Exception("Deserialized ASObject is not an activity");

		if (job.DeliverToFollowers)
			logger.LogDebug("Delivering activity {id} to followers", activity.Id);
		else
			logger.LogDebug("Delivering activity {id} to specified recipients", activity.Id);

		if (activity.Actor == null) throw new Exception("Actor must not be null");

		if (job is { DeliverToFollowers: false, RecipientIds.Count: < 1 })
			return;

		var query = job.DeliverToFollowers
			? db.Followings.Where(p => p.FolloweeId == job.ActorId)
			    .Select(p => new InboxQueryResult
				            { InboxUrl = p.FollowerSharedInbox ?? p.FollowerInbox, Host = p.FollowerHost })
			: db.Users.Where(p => job.RecipientIds.Contains(p.Id))
			    .Select(p => new InboxQueryResult { InboxUrl = p.SharedInbox ?? p.Inbox, Host = p.Host });

		// We want to deliver activities to the explicitly specified recipients first
		if (job is { DeliverToFollowers: true, RecipientIds.Count: > 0 }) {
			query = db.Users.Where(p => job.RecipientIds.Contains(p.Id))
			          .Select(p => new InboxQueryResult { InboxUrl = p.SharedInbox ?? p.Inbox, Host = p.Host })
			          .Concat(query);
		}

		var inboxQueryResults = await query.Where(p => p.InboxUrl != null && p.Host != null)
		                                   .Distinct()
		                                   .ToListAsync(cancellationToken: token);

		if (inboxQueryResults.Count == 0) return;

		var keypair = await db.UserKeypairs.FirstAsync(p => p.UserId == job.ActorId, cancellationToken: token);
		var payload = await activity.SignAndCompactAsync(keypair);

		foreach (var inboxQueryResult in inboxQueryResults)
			await queueSvc.DeliverQueue.EnqueueAsync(new DeliverJob {
				RecipientHost = inboxQueryResult.Host ?? throw new Exception("Recipient host must not be null"),
				InboxUrl      = inboxQueryResult.InboxUrl ?? throw new Exception("Recipient inboxUrl must not be null"),
				Payload       = payload,
				ContentType   = "application/activity+json",
				UserId        = job.ActorId
			});
	}

	private class InboxQueryResult {
		public required string? InboxUrl;
		public required string? Host;

		public bool Equals(InboxQueryResult? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return InboxUrl == other.InboxUrl && Host == other.Host;
		}
	}
}

[ProtoContract]
public class PreDeliverJob : Job {
	[ProtoMember(1)] public required string       SerializedActivity;
	[ProtoMember(2)] public required string       ActorId;
	[ProtoMember(3)] public required List<string> RecipientIds;
	[ProtoMember(4)] public required bool         DeliverToFollowers;
}