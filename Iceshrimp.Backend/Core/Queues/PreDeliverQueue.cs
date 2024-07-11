using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class PreDeliverQueue(int parallelism)
	: PostgresJobQueue<PreDeliverJobData>("pre-deliver", PreDeliverQueueProcessorDelegateAsync,
	                                      parallelism, TimeSpan.FromSeconds(60))
{
	private static async Task PreDeliverQueueProcessorDelegateAsync(
		Job job, PreDeliverJobData jobData, IServiceProvider scope, CancellationToken token
	)
	{
		var logger   = scope.GetRequiredService<ILogger<DeliverQueue>>();
		var db       = scope.GetRequiredService<DatabaseContext>();
		var queueSvc = scope.GetRequiredService<QueueService>();
		var config   = scope.GetRequiredService<IOptionsSnapshot<Config.SecuritySection>>();

		var parsed       = JToken.Parse(jobData.SerializedActivity);
		var expanded     = LdHelpers.Expand(parsed) ?? throw new Exception("Failed to expand activity");
		var deserialized = ASObject.Deserialize(expanded);
		if (deserialized is not ASActivity activity)
			throw new Exception("Deserialized ASObject is not an activity");

		if (jobData.DeliverToFollowers)
			logger.LogDebug("Delivering activity {id} to followers", activity.Id);
		else
			logger.LogDebug("Delivering activity {id} to specified recipients", activity.Id);

		if (activity.Actor == null) throw new Exception("Actor must not be null");

		if (jobData is { DeliverToFollowers: false, RecipientIds.Count: < 1 })
			return;

		var query = jobData.DeliverToFollowers
			? db.Followings.Where(p => p.FolloweeId == jobData.ActorId)
			    .Select(p => new InboxQueryResult
			    {
				    InboxUrl = p.FollowerSharedInbox ?? p.FollowerInbox, Host = p.FollowerHost
			    })
			: db.Users.Where(p => jobData.RecipientIds.Contains(p.Id))
			    .Select(p => new InboxQueryResult { InboxUrl = p.SharedInbox ?? p.Inbox, Host = p.Host });

		// We want to deliver activities to the explicitly specified recipients first
		if (jobData is { DeliverToFollowers: true, RecipientIds.Count: > 0 })
		{
			query = db.Users.Where(p => jobData.RecipientIds.Contains(p.Id))
			          .Select(p => new InboxQueryResult { InboxUrl = p.SharedInbox ?? p.Inbox, Host = p.Host })
			          .Concat(query);
		}

		var inboxQueryResults = await query.Where(p => p.InboxUrl != null && p.Host != null)
		                                   .Distinct()
		                                   .SkipDeadInstances(activity, db)
		                                   .SkipBlockedInstances(config.Value.FederationMode, db)
		                                   .ToListAsync(token);

		if (inboxQueryResults.Count == 0) return;

		string payload;
		if (config.Value.AttachLdSignatures)
		{
			var keypair = await db.UserKeypairs.FirstAsync(p => p.UserId == jobData.ActorId, token);
			payload = await activity.SignAndCompactAsync(keypair);
		}
		else
		{
			payload = activity.CompactToPayload();
		}

		foreach (var inboxQueryResult in inboxQueryResults)
			await queueSvc.DeliverQueue.EnqueueAsync(new DeliverJobData
			{
				RecipientHost =
					inboxQueryResult.Host ??
					throw new Exception("Recipient host must not be null"),
				InboxUrl =
					inboxQueryResult.InboxUrl ??
					throw new
						Exception("Recipient inboxUrl must not be null"),
				Payload     = payload,
				ContentType = "application/activity+json",
				UserId      = jobData.ActorId
			});
	}
}

file class InboxQueryResult : IEquatable<InboxQueryResult>
{
	public required string? Host;
	public required string? InboxUrl;

	public bool Equals(InboxQueryResult? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return InboxUrl == other.InboxUrl && Host == other.Host;
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((InboxQueryResult)obj);
	}

	[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification =
		                 "We are using this as a Tuple that works with LINQ on our IQueryable iterator. This is therefore intended behavior.")]
	public override int GetHashCode()
	{
		return HashCode.Combine(InboxUrl, Host);
	}

	public static bool operator ==(InboxQueryResult? left, InboxQueryResult? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(InboxQueryResult? left, InboxQueryResult? right)
	{
		return !Equals(left, right);
	}
}

file static class QueryableExtensions
{
	public static IQueryable<InboxQueryResult> SkipDeadInstances(
		this IQueryable<InboxQueryResult> query, ASActivity activity, DatabaseContext db
	)
	{
		return activity is ASFollow
			? query.Where(user => !db.Instances.Any(p => p.Host == user.Host && p.IsSuspended))
			: query.Where(user => !db.Instances.Any(p => p.Host == user.Host &&
			                                             ((p.IsNotResponding &&
			                                               p.LastCommunicatedAt <
			                                               DateTime.UtcNow - TimeSpan.FromDays(7)) ||
			                                              p.IsSuspended)));
	}

	public static IQueryable<InboxQueryResult> SkipBlockedInstances(
		this IQueryable<InboxQueryResult> query, Enums.FederationMode mode, DatabaseContext db
	)
	{
		// @formatter:off
		Expression<Func<InboxQueryResult, bool>> expr = mode switch
		{
			Enums.FederationMode.BlockList => u => u.Host == null || !db.BlockedInstances.Any(p => u.Host == p.Host || u.Host.EndsWith("." + p.Host)),
			Enums.FederationMode.AllowList => u => u.Host == null ||  db.AllowedInstances.Any(p => u.Host == p.Host || u.Host.EndsWith("." + p.Host)),

			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
		// @formatter:on

		return query.Where(expr);
	}
}

public class PreDeliverJobData
{
	[JR] [J("serializedActivity")] public required string       SerializedActivity { get; set; }
	[JR] [J("actorId")]            public required string       ActorId            { get; set; }
	[JR] [J("recipientIds")]       public required List<string> RecipientIds       { get; set; }
	[JR] [J("deliverToFollowers")] public required bool         DeliverToFollowers { get; set; }
}