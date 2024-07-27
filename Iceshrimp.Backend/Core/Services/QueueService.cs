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
using TaskExtensions = Iceshrimp.Backend.Core.Extensions.TaskExtensions;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService(
	IServiceScopeFactory scopeFactory,
	ILogger<QueueService> logger,
	IOptions<Config.QueueConcurrencySection> queueConcurrency,
	IHostApplicationLifetime lifetime
) : BackgroundService
{
	private readonly List<IPostgresJobQueue> _queues             = [];
	public readonly  BackgroundTaskQueue     BackgroundTaskQueue = new(queueConcurrency.Value.BackgroundTask);
	public readonly  DeliverQueue            DeliverQueue        = new(queueConcurrency.Value.Deliver);
	public readonly  InboxQueue              InboxQueue          = new(queueConcurrency.Value.Inbox);
	public readonly  PreDeliverQueue         PreDeliverQueue     = new(queueConcurrency.Value.PreDeliver);

	public IEnumerable<string> QueueNames => _queues.Select(p => p.Name);

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		_queues.AddRange([InboxQueue, PreDeliverQueue, DeliverQueue, BackgroundTaskQueue]);

		var tokenSource      = new CancellationTokenSource();
		var queueTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.ApplicationStopping);
		var queueToken       = queueTokenSource.Token;

		queueToken.Register(() =>
		{
			if (tokenSource.Token.IsCancellationRequested) return;
			logger.LogInformation("Shutting down queue processors...");
			tokenSource.CancelAfter(TimeSpan.FromSeconds(10));
		});

		tokenSource.Token.Register(() =>
		{
			PrepareForExit();
			logger.LogInformation("Queue shutdown complete.");
		});

		_ = Task.Run(ExecuteHealthchecksWorker, token);
		await Task.Run(ExecuteBackgroundWorkers, tokenSource.Token);

		return;

		async Task? ExecuteBackgroundWorkers()
		{
			var tasks = _queues.Select(queue => queue.ExecuteAsync(scopeFactory, tokenSource.Token, queueToken));
			await Task.WhenAll(tasks);
			await tokenSource.CancelAsync();
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

	private async Task PrepareForExitAsync()
	{
		// Move running tasks to the front of the queue
		await _queues.Select(queue => queue.RecoverOrPrepareForExitAsync()).AwaitAllAsync();
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
			                                            .SetProperty(i => i.DelayedUntil, _ => null)
			                                            .SetProperty(i => i.StartedAt, _ => null)
			                                            .SetProperty(i => i.FinishedAt, _ => null)
			                                            .SetProperty(i => i.Exception, _ => null)
			                                            .SetProperty(i => i.ExceptionMessage, _ => null)
			                                            .SetProperty(i => i.ExceptionSource, _ => null)
			                                            .SetProperty(i => i.StackTrace, _ => null));
			if (cnt <= 0) return;
			_queues.FirstOrDefault(p => p.Name == job.Queue)?.RaiseJobQueuedEvent();
		}
	}
}

public interface IPostgresJobQueue
{
	public string Name { get; }

	public TimeSpan Timeout { get; }

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
	private readonly SemaphorePlus         _semaphore      = new(parallelism);
	private readonly AsyncAutoResetEvent   _delayedChannel = new();
	private readonly AsyncAutoResetEvent   _queuedChannel  = new();
	private          ILogger<QueueService> _logger         = null!;
	private          IServiceScopeFactory  _scopeFactory   = null!;
	public           string                Name    => name;
	public           TimeSpan              Timeout => timeout;

	/*
	 * This is the main queue processor loop. The algorithm aims to only ever have as many workers running as needed,
	 * conserving resources.
	 *
	 * Before we explain the algorithm, a couple components need an explanation:
	 *
	 * The queuedChannel fires when a new task is queued, while the _delayedChannel fires when a task is delayed.
	 * These events are AsyncAutoResetEvents, meaning if they fire while no task is waiting for them to fire,
	 * they will "remember" that they fired until one runs .WaitAsync() on them, at which point they return immediately.
	 *
	 * There are two different CancellationTokens passed to this function. They are related to the two-step shutdown process.
	 * - 'queueToken' fires when the application begins shutting down.
	 *     This gives the queue processor a grace period, where it can finish currently running jobs,
	 *     without starting any new workers.
	 * - 'token' fires after the grace period, terminating any jobs that are still running.
	 *     They get reset to the 'queued' state at a later point during the shutdown process.
	 *
	 * The _semaphore object tracks how many jobs are currently running / how many more can be started before reaching
	 * the configured maximum concurrency for this queue. It also resolves a race condition that existed here previously,
	 * because counting the number of running jobs database-side is not atomic & therefore causes a ToC/ToU condition.
	 * 
	 * With that out of the way, the actual loop runs the following algorithm:
	 * 1. If either 'token' or 'queueToken' are canceled, wait for all remaining tasks to finish before returning.
	 * 2. Obtain a service scope & a corresponding database context
	 * 3. Compute the actualParallelism from the number of queued jobs & the available worker slots
	 * 4. If actualParallelism is 0, enter a branch:
	 * 4.1 If there is at least one queued job, and there are no available worker slots,
	 *     wait for a slot to become available and reset the loop
	 * 4.2 Otherwise, wait for a job to become queued and reset the loop
	 * 5. If it is likely that there are free worker slots after queueing actualParallelism jobs,
	 *    cancel 'queuedChannelCts' when _queuedChannel fires (using a special function which will *not* reset the event)
	 * 6. Start actualParallelism tasks (each containing one worker processing one job)
	 * 7. Reset the loop if any of the following conditions are met:
	 * 7.1 Any of the queued tasks finish
	 * 7.2 'queuedChannelCts' is canceled (see 5.)
	 * 7.3 'queueToken' is canceled, though not before:
	 * 7.3.1 All queued tasks finish, or 'token' is canceled (allowing for a two-step graceful/hard shutdown process)
	 *
	 * The shutdown process functions as follows:
	 * 1. While 'token' is not yet canceled, and there are still running jobs:
	 * 1.1 Wait for any job them to finish, or for 'token' to be canceled
	 */
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

				var queuedCount       = await db.GetJobQueuedCount(name, token);
				var actualParallelism = Math.Min(_semaphore.CurrentCount, queuedCount);

				if (actualParallelism == 0)
				{
					// Not doing this causes a TOC/TOU race condition, even if it'd likely only be a couple CPU cycles wide.
					if (_semaphore.CurrentCount == 0 && queuedCount > 0)
						await _semaphore.WaitAndReleaseAsync(token).SafeWaitAsync(queueToken);
					else
						await _queuedChannel.WaitAsync(token).SafeWaitAsync(queueToken);

					continue;
				}

				// ReSharper disable MethodSupportsCancellation
				var queuedChannelCts = new CancellationTokenSource();
				if (_semaphore.CurrentCount - queuedCount > 0)
				{
					_ = _queuedChannel.WaitWithoutResetAsync()
					                  .ContinueWith(_ => queuedChannelCts.Cancel())
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

		while (!token.IsCancellationRequested && _semaphore.ActiveCount > 0)
			await _semaphore.WaitAndReleaseAsync(CancellationToken.None).SafeWaitAsync(token);
	}

	public async Task RecoverOrPrepareForExitAsync()
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var cnt = await db.Jobs.Where(p => p.Status == Job.JobStatus.Running)
		                  .ExecuteUpdateAsync(p => p.SetProperty(i => i.Status, i => Job.JobStatus.Queued));

		if (cnt > 0) RaiseJobQueuedEvent();
	}

	private event EventHandler? QueuedChannelEvent;
	private event EventHandler? DelayedChannelEvent;

	public void RaiseJobQueuedEvent()  => QueuedChannelEvent?.Invoke(null, EventArgs.Empty);
	public void RaiseJobDelayedEvent() => DelayedChannelEvent?.Invoke(null, EventArgs.Empty);

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
					RaiseJobQueuedEvent();
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
			RaiseJobDelayedEvent();
		}
		else
		{
			var trigger = ts.Value;
			_ = Task.Run(async () =>
			{
				await using var bgScope = GetScope();
				await using var bgDb    = GetDbContext(bgScope);
				await Task.Delay(trigger - DateTime.UtcNow, token);
				RaiseJobDelayedEvent();
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

		if (await db.GetJob(name).ToListAsync(token) is not [{ } job])
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
				RaiseJobDelayedEvent();
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
		RaiseJobQueuedEvent();
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
		RaiseJobDelayedEvent();
	}
}