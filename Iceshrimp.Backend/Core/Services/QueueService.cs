using System.Text.Json;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService(IServiceScopeFactory scopeFactory) : BackgroundService
{
	private readonly List<IPostgresJobQueue> _queues             = [];
	public readonly  BackgroundTaskQueue     BackgroundTaskQueue = new();
	public readonly  DeliverQueue            DeliverQueue        = new();

	public readonly InboxQueue      InboxQueue      = new();
	public readonly PreDeliverQueue PreDeliverQueue = new();

	private async Task<NpgsqlConnection> GetNpgsqlConnection(IServiceScope scope)
	{
		var config     = scope.ServiceProvider.GetRequiredService<IOptions<Config.DatabaseSection>>();
		var dataSource = DatabaseContext.GetDataSource(config.Value);
		return await dataSource.OpenConnectionAsync();
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		_queues.AddRange([InboxQueue, PreDeliverQueue, DeliverQueue, BackgroundTaskQueue]);

		token.Register(RecoverOrPrepareForExit);

		_ = Task.Run(RegisterNotificationChannels, token);
		await Task.Run(ExecuteBackgroundWorkers, token);
		return;

		async Task? ExecuteBackgroundWorkers()
		{
			var tasks = _queues.Select(queue => queue.ExecuteAsync(scopeFactory, token));
			await Task.WhenAll(tasks);
		}

		async Task RegisterNotificationChannels()
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					await using var scope = scopeFactory.CreateAsyncScope();
					await using var conn  = await GetNpgsqlConnection(scope);

					conn.Notification += (_, args) =>
					{
						try
						{
							if (args.Channel is not "queued" and not "delayed") return;
							var queue = _queues.FirstOrDefault(p => p.Name == args.Payload);
							if (queue == null) return;

							if (args.Channel == "queued")
								queue.RaiseJobQueuedEvent();
							else
								queue.RaiseJobDelayedEvent();
						}
						catch
						{
							// ignored (errors will crash the host process)
						}
					};

					await using (var cmd = new NpgsqlCommand("LISTEN queued", conn))
					{
						await cmd.ExecuteNonQueryAsync(token);
					}

					await using (var cmd = new NpgsqlCommand("LISTEN delayed", conn))
					{
						await cmd.ExecuteNonQueryAsync(token);
					}

					while (!token.IsCancellationRequested)
					{
						await conn.WaitAsync(token);
					}
				}
				catch
				{
					// ignored (logging this would spam logs on restart)
				}
			}
		}
	}

	private async Task RecoverOrPrepareForExitAsync()
	{
		// Move running tasks to the front of the queue
		foreach (var queue in _queues) await queue.RecoverOrPrepareForExitAsync();
	}

	private void RecoverOrPrepareForExit()
	{
		RecoverOrPrepareForExitAsync().Wait();
	}
}

public interface IPostgresJobQueue
{
	public string Name { get; }

	public Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token);
	public Task RecoverOrPrepareForExitAsync();

	public void RaiseJobQueuedEvent();
	public void RaiseJobDelayedEvent();
}

public class PostgresJobQueue<T>(
	string name,
	Func<Job, T, IServiceProvider, CancellationToken, Task> handler,
	int parallelism
) : IPostgresJobQueue where T : class
{
	private readonly AsyncAutoResetEvent _delayedChannel = new(false);

	private readonly AsyncAutoResetEvent _queuedChannel = new(false);

	private IServiceScopeFactory _scopeFactory = null!;
	public  string               Name => name;

	public void RaiseJobQueuedEvent()  => QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
	public void RaiseJobDelayedEvent() => DelayedChannelEvent?.Invoke(null, EventArgs.Empty);

	public async Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token)
	{
		_scopeFactory = scopeFactory;
		await RecoverOrPrepareForExitAsync();

		QueuedChannelEvent  += (_, _) => _queuedChannel.Set();
		DelayedChannelEvent += (_, _) => _delayedChannel.Set();

		using var loggerScope = _scopeFactory.CreateScope();
		var       logger      = loggerScope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		_ = Task.Run(() => DelayedJobHandlerAsync(token), token);
		while (!token.IsCancellationRequested)
		{
			try
			{
				await using var scope = GetScope();
				await using var db    = GetDbContext(scope);

				var runningCount =
					await db.Jobs.CountAsync(p => p.Queue == name && p.Status == Job.JobStatus.Running,
					                         token);
				var queuedCount =
					await db.Jobs.CountAsync(p => p.Queue == name && p.Status == Job.JobStatus.Queued,
					                         token);

				var actualParallelism = Math.Min(parallelism - runningCount, queuedCount);
				if (actualParallelism == 0)
				{
					await _queuedChannel.WaitAsync(token);
					continue;
				}

				var tasks = new List<Task>();
				for (var i = 0; i < actualParallelism; i++) tasks.Add(ProcessJobAsync(token));

				await Task.WhenAny(tasks);
			}
			catch (Exception e)
			{
				if (!token.IsCancellationRequested)
				{
					logger.LogError("ExecuteAsync in queue {queue} failed with: {error}", name, e.Message);
					await Task.Delay(1000, token);
				}
			}
		}
	}

	public async Task RecoverOrPrepareForExitAsync()
	{
		//TODO: Make this support clustering
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);
		await db.Jobs.Where(p => p.Status == Job.JobStatus.Running)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, i => Job.JobStatus.Queued));
	}

	private event EventHandler? QueuedChannelEvent;
	private event EventHandler? DelayedChannelEvent;

	// ReSharper disable once SuggestBaseTypeForParameter
	private async Task RaiseJobQueuedEvent(DatabaseContext db) =>
		await db.Database.ExecuteSqlAsync($"SELECT pg_notify('queued', {name});");

	// ReSharper disable once SuggestBaseTypeForParameter
	private async Task RaiseJobDelayedEvent(DatabaseContext db) =>
		await db.Database.ExecuteSqlAsync($"SELECT pg_notify('delayed', {name});");

	private AsyncServiceScope GetScope() => _scopeFactory.CreateAsyncScope();

	private static DatabaseContext GetDbContext(IServiceScope scope) =>
		scope.ServiceProvider.GetRequiredService<DatabaseContext>();

	private async Task DelayedJobHandlerAsync(CancellationToken token)
	{
		using var loggerScope = _scopeFactory.CreateScope();
		var       logger      = loggerScope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		while (!token.IsCancellationRequested)
		{
			try
			{
				await using var scope = GetScope();
				await using var db    = GetDbContext(scope);

				var count = await db.Jobs
				                    .Where(p => p.Queue == name &&
				                                p.Status == Job.JobStatus.Delayed &&
				                                (p.DelayedUntil == null || p.DelayedUntil < DateTime.UtcNow))
				                    .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, i => Job.JobStatus.Queued),
				                                        token);

				if (count > 0)
				{
					await RaiseJobQueuedEvent(db);
					continue;
				}

				var tokenSource = new CancellationTokenSource();
				await ScheduleDelayedEvent(tokenSource.Token);
				await _delayedChannel.WaitAsync(token);
				await tokenSource.CancelAsync();
			}
			catch (Exception e)
			{
				if (!token.IsCancellationRequested)
				{
					logger.LogError("DelayedJobHandlerAsync in queue {queue} failed with: {error}", name, e.Message);
					await Task.Delay(1000, token);
				}
			}
		}
	}

	private async Task ScheduleDelayedEvent(CancellationToken token)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var ts = await db.Jobs
		                 .Where(p => p.Queue == name &&
		                             p.Status == Job.JobStatus.Delayed &&
		                             p.DelayedUntil != null)
		                 .OrderBy(p => p.DelayedUntil)
		                 .Select(p => p.DelayedUntil)
		                 .FirstOrDefaultAsync(token);

		if (!ts.HasValue) return;

		if (ts.Value < DateTime.UtcNow)
		{
			await RaiseJobDelayedEvent(db);
		}
		else
		{
			var trigger = ts.Value;
			_ = Task.Run(async () =>
			{
				await using var bgScope = GetScope();
				await using var bgDb    = GetDbContext(bgScope);
				await Task.Delay(trigger - DateTime.UtcNow, token);
				await RaiseJobDelayedEvent(bgDb);
			}, token);
		}
	}

	private async Task ProcessJobAsync(CancellationToken token)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var res = await db.Database.SqlQuery<Job>($"""
		                                           UPDATE "jobs" SET "status" = 'running', "started_at" = now()
		                                           WHERE "id" = (
		                                               SELECT "id" FROM "jobs"
		                                               WHERE status = 'queued' AND queue = {name}
		                                               ORDER BY COALESCE("delayed_until", "queued_at")
		                                               LIMIT 1
		                                               FOR UPDATE SKIP LOCKED)
		                                           RETURNING "jobs".*;
		                                           """)
		                  .ToListAsync(token);

		if (res is not [{ } job]) return;

		var data = JsonSerializer.Deserialize<T>(job.Data);
		if (data == null)
		{
			job.Status           = Job.JobStatus.Failed;
			job.ExceptionMessage = "Failed to deserialize job data";
			job.FinishedAt       = DateTime.UtcNow;
			db.Update(job);
			await db.SaveChangesAsync(token);
			return;
		}

		try
		{
			await handler(job, data, scope.ServiceProvider, token);
		}
		catch (Exception e)
		{
			job.Status           = Job.JobStatus.Failed;
			job.ExceptionMessage = e.Message;
			job.ExceptionSource  = e.TargetSite?.DeclaringType?.FullName ?? "Unknown";

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			if (e is GracefulException { Details: not null } ce)
			{
				logger.LogError("Failed to process job in {queue} queue: {error} - {details}",
				                name, ce.Message, ce.Details);
			}
			else
			{
				logger.LogError("Failed to process job in {queue} queue: {error}", name, e.Message);
			}
		}

		if (job.Status is Job.JobStatus.Failed)
		{
			job.FinishedAt = DateTime.UtcNow;
		}
		else if (job.Status is Job.JobStatus.Delayed)
		{
			if (job.DelayedUntil == null)
			{
				job.Status           = Job.JobStatus.Failed;
				job.ExceptionMessage = "Job marked as delayed but no until time set";
				job.ExceptionSource  = typeof(QueueService).FullName;
				job.FinishedAt       = DateTime.UtcNow;
			}
			else
			{
				var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
				logger.LogTrace("Job in queue {queue} was delayed to {time} after {duration} ms, has been queued since {time}",
				                name, job.DelayedUntil.Value.ToStringIso8601Like(), job.Duration,
				                job.QueuedAt.ToStringIso8601Like());
				db.Update(job);
				await db.SaveChangesAsync(token);
				await RaiseJobDelayedEvent(db);
				return;
			}
		}
		else
		{
			job.Status     = Job.JobStatus.Completed;
			job.FinishedAt = DateTime.UtcNow;

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			if (job.RetryCount == 0)
			{
				logger.LogTrace("Job in queue {queue} completed after {duration} ms, was queued for {queueDuration} ms",
				                name, job.Duration, job.QueueDuration);
			}
			else
			{
				logger.LogTrace("Job in queue {queue} completed after {duration} ms, has been queued since {time}",
				                name, job.Duration, job.QueuedAt.ToStringIso8601Like());
			}
		}

		db.Update(job);
		await db.SaveChangesAsync(token);

		await db.Jobs.Where(p => p.Queue == name && p.Status == Job.JobStatus.Completed)
		        .OrderByDescending(p => p.FinishedAt)
		        .Skip(10)
		        .ExecuteDeleteAsync(token);

		await db.Jobs.Where(p => p.Queue == name && p.Status == Job.JobStatus.Failed)
		        .OrderByDescending(p => p.FinishedAt)
		        .Skip(100)
		        .ExecuteDeleteAsync(token);
	}

	public async Task EnqueueAsync(T jobData)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var job = new Job { Queue = name, Data = JsonSerializer.Serialize(jobData) };
		db.Add(job);
		await db.SaveChangesAsync();
		await RaiseJobQueuedEvent(db);
	}

	public async Task ScheduleAsync(T jobData, DateTime triggerAt)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var job = new Job
		{
			Queue        = name,
			Data         = JsonSerializer.Serialize(jobData),
			Status       = Job.JobStatus.Delayed,
			DelayedUntil = triggerAt.ToUniversalTime()
		};
		db.Add(job);
		await db.SaveChangesAsync();

		await RaiseJobDelayedEvent(db);
	}
}