using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.Extensions.Options;
using ProtoBuf;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService : BackgroundService
{
	private readonly List<IJobQueue>             _queues = [];
	private readonly IServiceScopeFactory        _serviceScopeFactory;
	public readonly  JobQueue<BackgroundTaskJob> BackgroundTaskQueue;
	public readonly  JobQueue<DeliverJob>        DeliverQueue;
	public readonly  JobQueue<InboxJob>          InboxQueue;
	public readonly  JobQueue<PreDeliverJob>     PreDeliverQueue;

	public QueueService(
		IServiceScopeFactory serviceScopeFactory,
		IConnectionMultiplexer redis,
		IOptions<Config.InstanceSection> instanceConfig,
		IOptions<Config.RedisSection> redisConfig
	)
	{
		_serviceScopeFactory = serviceScopeFactory;
		var prefix = (redisConfig.Value.Prefix ?? instanceConfig.Value.WebDomain) + ":queue:";
		DeliverQueue        = Queues.DeliverQueue.Create(redis, prefix);
		InboxQueue          = Queues.InboxQueue.Create(redis, prefix);
		BackgroundTaskQueue = Queues.BackgroundTaskQueue.Create(redis, prefix);
		PreDeliverQueue     = Queues.PreDeliverQueue.Create(redis, prefix);
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		_queues.AddRange([InboxQueue, PreDeliverQueue, DeliverQueue, BackgroundTaskQueue]);

		await RecoverOrPrepareForExitAsync();
		token.Register(RecoverOrPrepareForExit);

		await Task.Run(ExecuteBackgroundWorkers, token);
		return;

		async Task? ExecuteBackgroundWorkers()
		{
			var tasks = _queues.Select(queue => queue.ExecuteAsync(_serviceScopeFactory, token));
			await Task.WhenAll(tasks);
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

public interface IJobQueue
{
	public Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token);
	public Task RecoverOrPrepareForExitAsync();
}

public class JobQueue<T>(
	string name,
	Func<T, IServiceProvider, CancellationToken, Task> handler,
	int parallelism,
	IConnectionMultiplexer redis,
	string prefix
) : IJobQueue where T : Job
{
	private readonly RedisChannel _delayedChannel = new(prefix + "channel:delayed", RedisChannel.PatternMode.Literal);
	private readonly RedisChannel _queuedChannel  = new(prefix + "channel:queued", RedisChannel.PatternMode.Literal);
	private readonly IDatabase    _redisDb        = redis.GetDatabase().WithKeyPrefix(prefix + name + ":");
	private readonly ISubscriber  _subscriber     = redis.GetSubscriber();

	public async Task ExecuteAsync(IServiceScopeFactory scopeFactory, CancellationToken token)
	{
		var logger = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		_ = Task.Run(() => DelayedJobHandlerAsync(scopeFactory, token), token);
		var channel = await _subscriber.SubscribeAsync(_queuedChannel);
		while (!token.IsCancellationRequested)
		{
			try
			{
				var actualParallelism = Math.Min(parallelism - await _redisDb.ListLengthAsync("running"),
				                                 await _redisDb.ListLengthAsync("queued"));
				if (actualParallelism == 0)
				{
					await channel.ReadAsync(token);
					continue;
				}

				var tasks = new List<Task>();
				for (var i = 0; i < actualParallelism; i++) tasks.Add(ProcessJobAsync(scopeFactory, token));

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
		while (await _redisDb.ListLengthAsync("running") > 0)
			await _redisDb.ListMoveAsync("running", "queued", ListSide.Right, ListSide.Left);
	}

	private async Task DelayedJobHandlerAsync(IServiceScopeFactory scopeFactory, CancellationToken token)
	{
		var channel = await _subscriber.SubscribeAsync(_queuedChannel);
		var logger  = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<QueueService>>();
		while (!token.IsCancellationRequested)
		{
			try
			{
				var timestamp = (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
				var res       = await _redisDb.SortedSetRangeByScoreAsync("delayed", 0, timestamp, take: 10);

				if (res.Length == 0)
				{
					await channel.ReadAsync(token);
					continue;
				}

				foreach (var item in res)
				{
					var transaction = _redisDb.CreateTransaction();
					_ = transaction.ListRightPushAsync("queued", item);
					_ = transaction.SortedSetRemoveAsync("delayed", item);
					await transaction.ExecuteAsync();
					await _subscriber.PublishAsync(_queuedChannel, "");
				}
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

	private async Task ProcessJobAsync(IServiceScopeFactory scopeFactory, CancellationToken token)
	{
		var res = await _redisDb.ListMoveAsync("queued", "running", ListSide.Left, ListSide.Right);
		if (res.IsNull || res.Box() is not byte[] buffer) return;
		var job = RedisHelpers.Deserialize<T>(buffer);
		if (job == null) return;
		job.Status    = Job.JobStatus.Running;
		job.StartedAt = DateTime.Now;
		var scope = scopeFactory.CreateScope();
		try
		{
			await handler(job, scope.ServiceProvider, token);
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
			job.FinishedAt = DateTime.Now;
			await _redisDb.ListRemoveAsync("running", res, 1);
			await _redisDb.ListRightPushAsync("failed", RedisValue.Unbox(RedisHelpers.Serialize(job)));
		}
		else if (job.Status is Job.JobStatus.Delayed)
		{
			if (job.DelayedUntil == null)
			{
				job.Status           = Job.JobStatus.Failed;
				job.ExceptionMessage = "Job marked as delayed but no until time set";
				job.ExceptionSource  = typeof(QueueService).FullName;
				job.FinishedAt       = DateTime.Now;
			}
		}
		else
		{
			job.Status     = Job.JobStatus.Completed;
			job.FinishedAt = DateTime.Now;

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

		var targetQueue = job.Status switch
		{
			Job.JobStatus.Completed => "completed",
			Job.JobStatus.Failed    => "failed",
			Job.JobStatus.Delayed   => "delayed",
			_                       => throw new Exception("ProcessJob: unknown job state on finish")
		};
		await _redisDb.ListRemoveAsync("running", res, 1);
		if (targetQueue == "delayed")
		{
			job.DelayedUntil = (job.DelayedUntil ?? DateTime.Now).ToLocalTime();

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			logger.LogTrace("Job in queue {queue} was delayed to {time} after {duration} ms, has been queued since {time}",
			                name, job.DelayedUntil.Value.ToStringIso8601Like(), job.Duration,
			                job.QueuedAt.ToStringIso8601Like());

			var timestamp = (long)job.DelayedUntil.Value.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
			await _redisDb.SortedSetAddAsync(targetQueue, RedisValue.Unbox(RedisHelpers.Serialize(job)), timestamp);
			await _subscriber.PublishAsync(_delayedChannel, "");
		}
		else
		{
			await _redisDb.ListLeftPushAsync(targetQueue, RedisValue.Unbox(RedisHelpers.Serialize(job)));
			await _redisDb.ListTrimAsync(targetQueue, 0, 9);
		}

		scope.Dispose();
	}

	public async Task EnqueueAsync(T job)
	{
		await _redisDb.ListRightPushAsync("queued", RedisValue.Unbox(RedisHelpers.Serialize(job)));
		await _subscriber.PublishAsync(_queuedChannel, "");
	}

	public async Task ScheduleAsync(T job, DateTime triggerAt)
	{
		job.Status       = Job.JobStatus.Delayed;
		job.DelayedUntil = triggerAt.ToLocalTime();
		var timestamp = (long)job.DelayedUntil.Value.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
		await _redisDb.SortedSetAddAsync("delayed", RedisValue.Unbox(RedisHelpers.Serialize(job)), timestamp);
		await _subscriber.PublishAsync(_delayedChannel, "");
	}
}

[ProtoContract]
[ProtoInclude(100, typeof(InboxJob))]
[ProtoInclude(101, typeof(DeliverJob))]
[ProtoInclude(102, typeof(PreDeliverJob))]
[ProtoInclude(103, typeof(BackgroundTaskJob))]
public abstract class Job
{
	public enum JobStatus
	{
		Queued,
		Delayed,
		Running,
		Completed,
		Failed
	}

	[ProtoMember(1)] public DateTime  QueuedAt = DateTime.Now;
	[ProtoMember(2)] public DateTime? StartedAt;
	[ProtoMember(3)] public DateTime? FinishedAt;
	[ProtoMember(4)] public DateTime? DelayedUntil;

	[ProtoMember(5)] public int RetryCount;

	[ProtoMember(10)] public string? ExceptionMessage;
	[ProtoMember(11)] public string? ExceptionSource;

	public JobStatus Status = JobStatus.Queued;
	public long      Duration      => (long)((FinishedAt ?? DateTime.Now) - (StartedAt ?? QueuedAt)).TotalMilliseconds;
	public long      QueueDuration => (long)((StartedAt ?? DateTime.Now) - QueuedAt).TotalMilliseconds;
}