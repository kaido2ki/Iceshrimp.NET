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

file record struct BackfillData(string Id, string RepliesCollection);

public class BackfillQueue(int parallelism)
	: PostgresJobQueue<BackfillJobData>("backfill", BackfillQueueProcessorDelegateAsync,
	                                    parallelism, TimeSpan.FromMinutes(10))
{
	public const int MaxRepliesPerThread = 1000;
	public const int MaxRepliesPerNote = 500;

	public static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});
	
	private static async Task BackfillQueueProcessorDelegateAsync(
		Job job,
		BackfillJobData jobData,
		IServiceProvider scope,
		CancellationToken token
	)
	{
		if (KeyedLocker.IsInUse(jobData.ThreadId)) return;
		using var _ = await KeyedLocker.LockAsync(jobData.ThreadId, token);
		
		var logger         = scope.GetRequiredService<ILogger<BackfillQueue>>();
		var backfillConfig = scope.GetRequiredService<IOptionsSnapshot<Config.BackfillSection>>();
		var db             = scope.GetRequiredService<DatabaseContext>();
		var noteSvc        = scope.GetRequiredService<NoteService>();
		var objectResolver = scope.GetRequiredService<ActivityPub.ObjectResolver>();
		
		var user = jobData.AuthenticatedUserId == null
			? null
			: await db.Users.Where(u => u.Id == jobData.AuthenticatedUserId).FirstOrDefaultAsync(token);
		
		logger.LogDebug("Backfilling replies for thread {id} as user {userId}", jobData.ThreadId, user?.Username);

		var cfg = backfillConfig.Value.Replies;
		var backfillLimit = MaxRepliesPerThread;
		var history = new HashSet<string>();
		
		var toBackfillArray = await db.Notes
		                         .Where(n => n.ThreadId == jobData.ThreadId
		                                     && n.RepliesCount < MaxRepliesPerNote
		                                     && n.UserHost != null
		                                     && n.RepliesCollection != null 
		                                     && n.CreatedAt <= DateTime.UtcNow - cfg.NewNoteDelayTimeSpan 
		                                     && (n.RepliesFetchedAt == null || n.RepliesFetchedAt <= DateTime.UtcNow - cfg.RefreshAfterTimeSpan))
		                         .Select(n => new BackfillData(n.Id, n.RepliesCollection!))
		                         .ToArrayAsync(token);
		
		var toBackfill = new Queue<BackfillData>(toBackfillArray);
		while (toBackfill.TryDequeue(out var _current))
		{
			var current = _current;
			if (!history.Add(current.RepliesCollection)) 
			{
				logger.LogDebug("Skipping {collection} as it was already backfilled in this run", current.RepliesCollection);
				continue;
			}

			logger.LogTrace("Backfilling {collection} (remaining limit {limit})", current.RepliesCollection, backfillLimit);
			
			await db.Notes
			        .Where(n => n.Id == current.Id)
			        .ExecuteUpdateAsync(p => p.SetProperty(n => n.RepliesFetchedAt, DateTime.UtcNow), token);

			await foreach (var asNote in objectResolver.IterateCollection(new ASCollection(current.RepliesCollection))
			                                             .Take(MaxRepliesPerNote)
			                                             .Where(p => p.Id != null)
														 .WithCancellation(token))
			{
				if (--backfillLimit <= 0) 
				{
					logger.LogDebug("Reached backfill limit");
					toBackfill.Clear();
					break;
				}
				
				var note = await noteSvc.ResolveNoteAsync(asNote.Id!, asNote as ASNote, user, clearHistory: true, forceRefresh: false);
				
				if (note is { UserHost: not null, RepliesCollection: not null, RepliesCount: < MaxRepliesPerNote }
				    && note.CreatedAt <= DateTime.UtcNow - cfg.NewNoteDelayTimeSpan 
				    && (note.RepliesFetchedAt == null || note.RepliesFetchedAt <= DateTime.UtcNow - cfg.RefreshAfterTimeSpan))
				{
					toBackfill.Enqueue(new BackfillData(note.Id, note.RepliesCollection!));
				}
			}
		}

		await db.NoteThreads
			.Where(t => t.Id == jobData.ThreadId)
			.ExecuteUpdateAsync(p => p.SetProperty(t => t.BackfilledAt, DateTime.UtcNow), cancellationToken: default);
	}
}

public class BackfillJobData
{
	[JR] [J("threadId")]            public required string  ThreadId            { get; set; }
	[JR] [J("authenticatedUserId")] public required string? AuthenticatedUserId { get; set; }
}