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
	private readonly List<IPostgresJobQueue> _queues             = [];
	public readonly  BackgroundTaskQueue     BackgroundTaskQueue = new(queueConcurrency.Value.BackgroundTask);
	public readonly  DeliverQueue            DeliverQueue        = new(queueConcurrency.Value.Deliver);
	public readonly  InboxQueue              InboxQueue          = new(queueConcurrency.Value.Inbox);
	public readonly  PreDeliverQueue         PreDeliverQueue     = new(queueConcurrency.Value.PreDeliver);

	public IEnumerable<string> QueueNames => _queues.Select(p => p.Name);

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
		_ = Task.Run(ExecuteHealthchecksWorker, token);
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
								queue.RaiseLocalJobQueuedEvent();
							else
								queue.RaiseLocalJobDelayedEvent();
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

		async Task ExecuteHealthchecksWorker()
		{
			var first = true;
			while (!token.IsCancellationRequested)
			{
				try
				{
					if (!first) await Task.Delay(TimeSpan.FromMinutes(5), token);
					else first = false;
					await using var scope = scopeFactory.CreateAsyncScope();
					await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
					foreach (var queue in _queues)
					{
						var cnt =
							await db.Jobs.Where(p => p.Status == Job.JobStatus.Running &&
							                         p.Queue == queue.Name &&
							                         p.StartedAt < DateTime.UtcNow - queue.Timeout * 2)
							        .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, _ => Job.JobStatus.Failed)
							                                  .SetProperty(i => i.FinishedAt, _ => DateTime.UtcNow)
							                                  .SetProperty(i => i.ExceptionMessage,
							                                               _ => "Worker stalled")
							                                  .SetProperty(i => i.ExceptionSource,
							                                               _ => "HealthchecksWorker"),
							                            token);

						if (cnt <= 0) continue;

						var jobs = await db.Jobs
						                   .Where(p => p.Status == Job.JobStatus.Failed &&
						                               p.Queue == queue.Name &&
						                               p.FinishedAt > DateTime.UtcNow - TimeSpan.FromSeconds(30) &&
						                               p.ExceptionMessage == "Worker stalled" &&
						                               p.ExceptionSource == "HealthchecksWorker")
						                   .Select(p => p.Id)
						                   .ToListAsync(token);

						logger.LogWarning("Healthchecks worker cleaned up {count} stalled jobs in queue {name}:\n- {jobs}",
						                  cnt, queue.Name, string.Join("\n- ", jobs));
					}
				}
				catch (Exception e)
				{
					if (!token.IsCancellationRequested)
						logger.LogWarning("Healthchecks worker failed with {error}, restarting...", e.Message);
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

	public async Task RetryJobAsync(Job job)
	{
		await using var scope = scopeFactory.CreateAsyncScope();
		await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
		if (job.Status == Job.JobStatus.Failed)
		{
			var cnt = await db.Jobs.Where(p => p.Id == job.Id && p.Status == Job.JobStatus.Failed)
			                  .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, _ => Job.JobStatus.Queued)
			                                            .SetProperty(i => i.QueuedAt, _ => DateTime.UtcNow)
			                                            .SetProperty(i => i.RetryCount, i => i.RetryCount + 1)
			                                            .SetProperty(i => i.WorkerId, _ => null)
			                                            .SetProperty(i => i.DelayedUntil, _ => null)
			                                            .SetProperty(i => i.StartedAt, _ => null)
			                                            .SetProperty(i => i.FinishedAt, _ => null)
			                                            .SetProperty(i => i.Exception, _ => null)
			                                            .SetProperty(i => i.ExceptionMessage, _ => null)
			                                            .SetProperty(i => i.ExceptionSource, _ => null)
			                                            .SetProperty(i => i.StackTrace, _ => null));
			if (cnt <= 0) return;
			var queue = _queues.FirstOrDefault(p => p.Name == job.Queue);
			if (queue != null) await queue.RaiseJobQueuedEvent(db);
		}
	}
}

public interface IPostgresJobQueue
{
	public string Name { get; }

	public TimeSpan Timeout { get; }

	public Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token, CancellationToken queueToken);
	public Task RecoverOrPrepareForExitAsync();
	public Task RaiseJobQueuedEvent(DatabaseContext db);
	public Task RaiseJobDelayedEvent(DatabaseContext db);

	public void RaiseLocalJobQueuedEvent();
	public void RaiseLocalJobDelayedEvent();
}

public class PostgresJobQueue<T>(
	string name,
	Func<Job, T, IServiceProvider, CancellationToken, Task> handler,
	int parallelism,
	TimeSpan timeout
) : IPostgresJobQueue where T : class
{
	private readonly SemaphorePlus         _semaphore      = new(parallelism);
	private readonly AsyncAutoResetEvent   _delayedChannel = new();
	private readonly AsyncAutoResetEvent   _queuedChannel  = new();
	private          ILogger<QueueService> _logger         = null!;
	private          IServiceScopeFactory  _scopeFactory   = null!;
	private          string?               _workerId;
	public           string                Name    => name;
	public           TimeSpan              Timeout => timeout;

	public void RaiseLocalJobQueuedEvent()  => QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
	public void RaiseLocalJobDelayedEvent() => DelayedChannelEvent?.Invoke(null, EventArgs.Empty);

	public async Task ExecuteAsync(
		IServiceScopeFactory scopeFactory, CancellationToken token, CancellationToken queueToken
	)
	{
		_scopeFactory = scopeFactory;
		_logger       = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		await RecoverOrPrepareForExitAsync();

		QueuedChannelEvent  += (_, _) => _queuedChannel.Set();
		DelayedChannelEvent += (_, _) => _delayedChannel.Set();

		using var loggerScope = _scopeFactory.CreateScope();
		_ = Task.Run(() => DelayedJobHandlerAsync(token), token);
		while (!token.IsCancellationRequested && !queueToken.IsCancellationRequested)
		{
			try
			{
				await using var scope = GetScope();
				await using var db    = GetDbContext(scope);

				var queuedCount       = await db.GetJobQueuedCount(name, _workerId, token);
				var actualParallelism = Math.Min(parallelism - _semaphore.ActiveCount, queuedCount);

				if (actualParallelism <= 0)
				{
					await _queuedChannel.WaitAsync(token).SafeWaitAsync(queueToken);
					continue;
				}

				// ReSharper disable MethodSupportsCancellation
				var queuedChannelCts = new CancellationTokenSource();
				if (_semaphore.ActiveCount + queuedCount < parallelism)
				{
					_ = _queuedChannel.WaitWithoutResetAsync()
					                  .ContinueWith(_ =>
					                  {
						                  if (_semaphore.ActiveCount < parallelism)
							                  queuedChannelCts.Cancel();
					                  })
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
					_logger.LogError("ExecuteAsync for queue {queue} failed with: {error}", name, e);
					await Task.Delay(1000, token);
					_logger.LogDebug("Restarting ExecuteAsync worker for queue {queue}", name);
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
	public async Task RaiseJobQueuedEvent(DatabaseContext db)
	{
		if (_workerId == null)
			QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
		else
			await db.Database.ExecuteSqlAsync($"SELECT pg_notify('queued', {name});");
	}

	// ReSharper disable once SuggestBaseTypeForParameter
	public async Task RaiseJobDelayedEvent(DatabaseContext db)
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
					_logger.LogError("DelayedJobHandlerAsync for queue {queue} failed with: {error}", name, e);
					await Task.Delay(1000, token);
					_logger.LogDebug("Restarting DelayedJobHandlerAsync worker for queue {queue}", name);
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
		await using var processorScope = GetScope();
		await using var jobScope       = GetScope();
		try
		{
			await _semaphore.WaitAsync(CancellationToken.None).SafeWaitAsync(token);
			await ProcessJobAsync(processorScope, jobScope, token);
		}
		catch (Exception e)
		{
			_logger.LogError("ProcessJobAsync for queue {queue} failed with: {error}", name, e);
			_logger.LogError("Queue worker(s) for queue {queue} might be degraded or stalled. Please report this bug to the developers.",
			                 name);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	private async Task ProcessJobAsync(IServiceScope processorScope, IServiceScope jobScope, CancellationToken token)
	{
		await using var db = GetDbContext(processorScope);

		if (await db.GetJob(name, _workerId).ToListAsync(token) is not [{ } job])
			return;

		_logger.LogTrace("Processing {queue} job {id}", name, job.Id);

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
			await handler(job, data, jobScope.ServiceProvider, token).WaitAsync(timeout, token);
		}
		catch (Exception e)
		{
			job.Status           = Job.JobStatus.Failed;
			job.ExceptionMessage = e.Message;
			job.ExceptionSource  = e.TargetSite?.DeclaringType?.FullName ?? "Unknown";
			job.StackTrace       = e.StackTrace;
			job.Exception        = e.ToString();

			var queueName = data is BackgroundTaskJobData ? name + $" ({data.GetType().Name})" : name;
			if (e is GracefulException { Details: not null } ce)
			{
				_logger.LogError("Failed to process job {id} in queue {queue}: {error} - {details}",
				                 job.Id.ToStringLower(), queueName, ce.Message, ce.Details);
			}
			else if (e is TimeoutException)
			{
				_logger.LogError("Job {id} in queue {queue} didn't complete within the configured timeout ({timeout} seconds)",
				                 job.Id.ToStringLower(), queueName, (int)timeout.TotalSeconds);
			}
			else
			{
				_logger.LogError(e, "Failed to process job {id} in queue {queue}: {error}",
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
				_logger.LogTrace("Job {id} in queue {queue} was delayed to {time} after {duration} ms, has been queued since {time}",
				                 job.Id, name, job.DelayedUntil.Value.ToLocalTime().ToStringIso8601Like(), job.Duration,
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

			if (job.RetryCount == 0)
			{
				_logger.LogTrace("Job {id} in queue {queue} completed after {duration} ms, was queued for {queueDuration} ms",
				                 job.Id, name, job.Duration, job.QueueDuration);
			}
			else
			{
				_logger.LogTrace("Job {id} in queue {queue} completed after {duration} ms, has been queued since {time}",
				                 job.Id, name, job.Duration, job.QueuedAt.ToStringIso8601Like());
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