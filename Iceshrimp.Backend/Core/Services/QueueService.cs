using System.Linq.Expressions;
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
using TaskExtensions = Iceshrimp.Backend.Core.Extensions.TaskExtensions;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService(
	IServiceScopeFactory scopeFactory,
	ILogger<QueueService> logger,
	IOptions<Config.WorkerSection> config,
	IOptions<Config.QueueConcurrencySection> queueConcurrency
) : BackgroundService
{
	private readonly List<IPostgresJobQueue> _queues = [];

	public IEnumerable<string> QueueNames => _queues.Select(p => p.Name);

	public readonly InboxQueue          InboxQueue          = new(queueConcurrency.Value.Inbox);
	public readonly DeliverQueue        DeliverQueue        = new(queueConcurrency.Value.Deliver);
	public readonly PreDeliverQueue     PreDeliverQueue     = new(queueConcurrency.Value.PreDeliver);
	public readonly BackgroundTaskQueue BackgroundTaskQueue = new(queueConcurrency.Value.BackgroundTask);

	private static async Task<NpgsqlConnection> GetNpgsqlConnection(IServiceScope scope)
	{
		var config     = scope.ServiceProvider.GetRequiredService<IOptions<Config.DatabaseSection>>();
		var dataSource = DatabaseContext.GetDataSource(config.Value);
		return await dataSource.OpenConnectionAsync();
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		_queues.AddRange([InboxQueue, PreDeliverQueue, DeliverQueue, BackgroundTaskQueue]);

		var cts = new CancellationTokenSource();

		token.Register(() =>
		{
			if (cts.Token.IsCancellationRequested) return;
			logger.LogInformation("Shutting down queue processors...");
			cts.CancelAfter(TimeSpan.FromSeconds(10));
		});

		cts.Token.Register(() =>
		{
			PrepareForExit();
			logger.LogInformation("Queue shutdown complete.");
		});

		_ = Task.Run(RegisterNotificationChannels, token);
		_ = Task.Run(ExecuteHeartbeatWorker, token);
		await Task.Run(ExecuteBackgroundWorkers, cts.Token);
		return;

		async Task? ExecuteBackgroundWorkers()
		{
			var tasks = _queues.Select(queue => queue.ExecuteAsync(scopeFactory, cts.Token, token));
			await Task.WhenAll(tasks);
			await cts.CancelAsync();
		}

		async Task RegisterNotificationChannels()
		{
			if (config.Value.WorkerId == null) return;

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
					// ignored (logging this would spam logs on postgres restart)
				}
			}
		}

		async Task ExecuteHeartbeatWorker()
		{
			if (config.Value.WorkerId == null) return;

			while (!token.IsCancellationRequested)
			{
				try
				{
					await using var scope = scopeFactory.CreateAsyncScope();
					await using var conn  = await GetNpgsqlConnection(scope);

					var sql = $"""
					           INSERT INTO "worker" ("id", "heartbeat")
					           VALUES ({config.Value.WorkerId}, now())
					           ON CONFLICT ("id")
					               DO UPDATE
					               SET "heartbeat" = now();
					           """;
					while (!token.IsCancellationRequested)
					{
						await using var cmd = new NpgsqlCommand(sql, conn);
						await cmd.ExecuteNonQueryAsync(token);
						await Task.Delay(TimeSpan.FromSeconds(30), token);
					}
				}
				catch
				{
					// ignored (logging this would spam logs on postgres restart)
				}
			}
		}
	}

	private async Task RemoveWorkerEntry()
	{
		if (config.Value.WorkerId == null) return;

		try
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			await using var conn  = await GetNpgsqlConnection(scope);

			var sql = $"""
			           DELETE FROM "worker" WHERE "id" = {config.Value.WorkerId}::varchar;
			           """;
			await using var cmd = new NpgsqlCommand(sql, conn);
			await cmd.ExecuteNonQueryAsync();
		}
		catch (Exception e)
		{
			logger.LogWarning("Failed to remove worker entry from database: {e}", e.Message);
		}
	}

	private async Task PrepareForExitAsync()
	{
		// Move running tasks to the front of the queue
		foreach (var queue in _queues) await queue.RecoverOrPrepareForExitAsync();
		await RemoveWorkerEntry();
	}

	private void PrepareForExit()
	{
		PrepareForExitAsync().Wait();
	}
}

public interface IPostgresJobQueue
{
	public string Name { get; }

	public Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token, CancellationToken queueToken);
	public Task RecoverOrPrepareForExitAsync();

	public void RaiseJobQueuedEvent();
	public void RaiseJobDelayedEvent();
}

public class PostgresJobQueue<T>(
	string name,
	Func<Job, T, IServiceProvider, CancellationToken, Task> handler,
	int parallelism,
	TimeSpan timeout
) : IPostgresJobQueue where T : class
{
	private readonly AsyncAutoResetEvent  _delayedChannel = new();
	private readonly AsyncAutoResetEvent  _queuedChannel  = new();
	private          IServiceScopeFactory _scopeFactory   = null!;
	public           string               Name => name;
	private          string?              _workerId;

	public void RaiseJobQueuedEvent()  => QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
	public void RaiseJobDelayedEvent() => DelayedChannelEvent?.Invoke(null, EventArgs.Empty);

	public async Task ExecuteAsync(
		IServiceScopeFactory scopeFactory, CancellationToken token, CancellationToken queueToken
	)
	{
		_scopeFactory = scopeFactory;
		await RecoverOrPrepareForExitAsync();

		QueuedChannelEvent  += (_, _) => _queuedChannel.Set();
		DelayedChannelEvent += (_, _) => _delayedChannel.Set();

		using var loggerScope = _scopeFactory.CreateScope();
		var       logger      = loggerScope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		_ = Task.Run(() => DelayedJobHandlerAsync(token), token);
		while (!token.IsCancellationRequested && !queueToken.IsCancellationRequested)
		{
			try
			{
				await using var scope = GetScope();
				await using var db    = GetDbContext(scope);

				var runningCount = _workerId == null
					? await db.Jobs.CountAsync(p => p.Queue == name && p.Status == Job.JobStatus.Running, token)
					: await db.Jobs.CountAsync(p => p.Queue == name &&
					                                p.Status == Job.JobStatus.Running &&
					                                p.WorkerId != null &&
					                                p.WorkerId == _workerId, token);
				var queuedCount = _workerId == null
					? await db.Jobs.CountAsync(p => p.Queue == name && p.Status == Job.JobStatus.Queued, token)
					: await
						db.Jobs.CountAsync(p => p.Queue == name &&
						                        (p.Status == Job.JobStatus.Queued ||
						                         (p.Status == Job.JobStatus.Running &&
						                          p.WorkerId != null &&
						                          !db.Workers.Any(w => w.Id == p.WorkerId &&
						                                               w.Heartbeat >
						                                               DateTime.UtcNow - TimeSpan.FromSeconds(90)))),
						                   token);

				var actualParallelism = Math.Min(parallelism - runningCount, queuedCount);
				if (actualParallelism <= 0)
				{
					await _queuedChannel.WaitAsync(token).SafeWaitAsync(queueToken);
					continue;
				}

				// ReSharper disable MethodSupportsCancellation
				var queuedChannelCts = new CancellationTokenSource();
				if (actualParallelism < parallelism)
				{
					_ = _queuedChannel.WaitWithoutResetAsync()
					                  .ContinueWith(_ => { queuedChannelCts.Cancel(); })
					                  .SafeWaitAsync(queueToken);
				}
				// ReSharper restore MethodSupportsCancellation

				var tasks = TaskExtensions.QueueMany(() => AttemptProcessJobAsync(token), actualParallelism);
				await Task.WhenAny(tasks)
				          .SafeWaitAsync(queuedChannelCts.Token)
				          .SafeWaitAsync(queueToken, () => Task.WhenAll(tasks).WaitAsync(token));
			}
			catch (Exception e)
			{
				if (!token.IsCancellationRequested)
				{
					logger.LogError("ExecuteAsync for queue {queue} failed with: {error}", name, e);
					await Task.Delay(1000, token);
					logger.LogDebug("Restarting ExecuteAsync worker for queue {queue}", name);
				}
			}
		}
	}

	public async Task RecoverOrPrepareForExitAsync()
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);
		_workerId = scope.ServiceProvider.GetRequiredService<IOptions<Config.WorkerSection>>().Value.WorkerId;

		Expression<Func<Job, bool>> predicate = _workerId == null
			? p => p.Status == Job.JobStatus.Running
			: p => p.Status == Job.JobStatus.Running && p.WorkerId != null && p.WorkerId == _workerId;

		var cnt = await db.Jobs.Where(predicate)
		                  .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, i => Job.JobStatus.Queued));

		if (cnt > 0) await RaiseJobQueuedEvent(db);
	}

	private event EventHandler? QueuedChannelEvent;
	private event EventHandler? DelayedChannelEvent;

	// ReSharper disable once SuggestBaseTypeForParameter
	private async Task RaiseJobQueuedEvent(DatabaseContext db)
	{
		if (_workerId == null)
			QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
		else
			await db.Database.ExecuteSqlAsync($"SELECT pg_notify('queued', {name});");
	}

	// ReSharper disable once SuggestBaseTypeForParameter
	private async Task RaiseJobDelayedEvent(DatabaseContext db)
	{
		if (_workerId == null)
			DelayedChannelEvent?.Invoke(null, EventArgs.Empty);
		else
			await db.Database.ExecuteSqlAsync($"SELECT pg_notify('delayed', {name});");
	}

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
					logger.LogError("DelayedJobHandlerAsync for queue {queue} failed with: {error}", name, e);
					await Task.Delay(1000, token);
					logger.LogDebug("Restarting DelayedJobHandlerAsync worker for queue {queue}", name);
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

	private async Task AttemptProcessJobAsync(CancellationToken token)
	{
		await using var scope = GetScope();
		try
		{
			await ProcessJobAsync(scope, token);
		}
		catch (Exception e)
		{
			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			logger.LogError("ProcessJobAsync for queue {queue} failed with: {error}", name, e);
			logger.LogError("Queue worker(s) for queue {queue} might be degraded or stalled. Please report this bug to the developers.",
			                name);
		}
	}

	private async Task ProcessJobAsync(IServiceScope scope, CancellationToken token)
	{
		await using var db = GetDbContext(scope);

		if (await db.GetJobs(name, _workerId).ToListAsync(token) is not [{ } job])
			return;

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
			await handler(job, data, scope.ServiceProvider, token).WaitAsync(timeout, token);
		}
		catch (Exception e)
		{
			job.Status           = Job.JobStatus.Failed;
			job.ExceptionMessage = e.Message;
			job.ExceptionSource  = e.TargetSite?.DeclaringType?.FullName ?? "Unknown";

			var logger    = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			var queueName = data is BackgroundTaskJobData ? name + $" ({data.GetType().Name})" : name;
			if (e is GracefulException { Details: not null } ce)
			{
				logger.LogError("Failed to process job {id} in queue {queue}: {error} - {details}",
				                job.Id.ToStringLower(), queueName, ce.Message, ce.Details);
			}
			else if (e is TimeoutException)
			{
				logger.LogError("Job {id} in queue {queue} didn't complete within the configured timeout ({timeout} seconds)",
				                job.Id.ToStringLower(), queueName, (int)timeout.TotalSeconds);
			}
			else
			{
				logger.LogError(e, "Failed to process job {id} in queue {queue}: {error}",
				                job.Id.ToStringLower(), queueName, e);
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
				                name, job.DelayedUntil.Value.ToLocalTime().ToStringIso8601Like(), job.Duration,
				                job.QueuedAt.ToLocalTime().ToStringIso8601Like());
				db.ChangeTracker.Clear();
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

		db.ChangeTracker.Clear();
		db.Update(job);
		await db.SaveChangesAsync(token);
	}

	public async Task EnqueueAsync(T jobData)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var job = new Job
		{
			Id    = Ulid.NewUlid().ToGuid(),
			Queue = name,
			Data  = JsonSerializer.Serialize(jobData)
		};
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
			Id           = Ulid.NewUlid().ToGuid(),
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