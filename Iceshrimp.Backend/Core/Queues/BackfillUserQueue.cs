using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;

namespace Iceshrimp.Backend.Core.Queues;

public class BackfillUserQueue(int parallelism)
	: PostgresJobQueue<BackfillUserJobData>("backfill-user", BackfillUserQueueProcessorDelegateAsync,
	                                    parallelism, TimeSpan.FromMinutes(10))
{
	public static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private static async Task BackfillUserQueueProcessorDelegateAsync(
		Job job,
		BackfillUserJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		if (KeyedLocker.IsInUse(jobData.UserId)) return;
		using var _ = await KeyedLocker.LockAsync(jobData.UserId, token);

		var logger         = scope.GetRequiredService<ILogger<BackfillUserQueue>>();
		var backfillConfig = scope.GetRequiredService<IOptionsSnapshot<Config.BackfillSection>>();
		var db             = scope.GetRequiredService<DatabaseContext>();
		var objectResolver = scope.GetRequiredService<ActivityPub.ObjectResolver>();
		var apHandler      = scope.GetRequiredService<ActivityPub.ActivityHandlerService>();

		var cfg = backfillConfig.Value.User;
		var history = new HashSet<string>();

		var toBackfill = await db.Users
		                         .Where(u => u.Id == jobData.UserId
											 && !u.Followers.Any()
		                                     && u.Outbox != null
		                                     && (u.OutboxFetchedAt == null || u.OutboxFetchedAt <= DateTime.UtcNow - cfg.RefreshAfterTimeSpan))
		                         .Select(n => new { n.Id, n.Outbox })
		                         .FirstOrDefaultAsync(token);

		if (toBackfill?.Outbox == null) return;
		logger.LogDebug("Backfilling outbox for user {id}", toBackfill.Id);

		await foreach (var asObject in objectResolver.IterateCollection(new ASCollection(toBackfill.Outbox))
													 .Take(cfg.MaxItems)
													 .Where(p => p.Id != null)
													 .WithCancellation(token))
		{
			if (!history.Add(asObject.Id!))
			{
				logger.LogDebug("Skipping {object} as it was already backfilled in this run", asObject.Id);
				continue;
			}

			if (asObject is not ASActivity activity)
			{
				logger.LogDebug("Object {object} of type {type} is not a valid activity", asObject.Id, asObject.Type);
				continue;
			}

			if (asObject is not (ASCreate or ASAnnounce or ASLike or ASEmojiReact))
			{
				logger.LogDebug("Activity {object} of type {type} is not allowed to be backfilled", asObject.Id, asObject.Type);
				continue;
			}

			try
			{
				await apHandler.PerformActivityAsync(activity, null, toBackfill.Id);
			}
			catch (Exception e)
			{
				logger.LogWarning(e, "Failed to backfill {activity}", activity.Id);
			}
		}

		await db.Users
				.Where(n => n.Id == jobData.UserId)
				.ExecuteUpdateAsync(p => p.SetProperty(n => n.OutboxFetchedAt, DateTime.UtcNow), token);
	}
}

public class BackfillUserJobData
{
	[JR] [J("userId")] public required string UserId { get; set; }
}
