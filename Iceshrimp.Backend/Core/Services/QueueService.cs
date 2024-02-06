using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Microsoft.Extensions.Options;
using ProtoBuf;
using StackExchange.Redis;
using StackExchange.Redis.KeyspaceIsolation;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService : BackgroundService {
	private readonly List<IJobQueue>      _queues = [];
	private readonly IServiceScopeFactory _serviceScopeFactory;
	public readonly  JobQueue<DeliverJob> DeliverQueue;
	public readonly  JobQueue<InboxJob>   InboxQueue;

	public QueueService(
		IServiceScopeFactory serviceScopeFactory,
		IConnectionMultiplexer redis,
		IOptions<Config.InstanceSection> instanceConfig,
		IOptions<Config.RedisSection> redisConfig
	) {
		_serviceScopeFactory = serviceScopeFactory;
		var prefix = (redisConfig.Value.Prefix ?? instanceConfig.Value.WebDomain) + ":queue:";
		DeliverQueue = Queues.DeliverQueue.Create(redis, prefix);
		InboxQueue   = Queues.InboxQueue.Create(redis, prefix);
	}

	protected override async Task ExecuteAsync(CancellationToken token) {
		_queues.AddRange([InboxQueue, DeliverQueue]);

		await RecoverOrPrepareForExitAsync();
		token.Register(RecoverOrPrepareForExit);

		while (!token.IsCancellationRequested) {
			foreach (var _ in _queues.Select(queue => queue.TickAsync(_serviceScopeFactory, token))) { }

			await Task.Delay(100, token);
		}
	}

	private async Task RecoverOrPrepareForExitAsync() {
		// Move running tasks to the front of the queue
		foreach (var queue in _queues) await queue.RecoverOrPrepareForExitAsync();
	}

	private void RecoverOrPrepareForExit() {
		RecoverOrPrepareForExitAsync().Wait();
	}
}

public interface IJobQueue {
	public Task TickAsync(IServiceScopeFactory scopeFactory, CancellationToken token);
	public Task RecoverOrPrepareForExitAsync();
}

public class JobQueue<T>(
	string name,
	Func<T, IServiceProvider, CancellationToken, Task> handler,
	int parallelism,
	IConnectionMultiplexer redis,
	string prefix
) : IJobQueue where T : Job {
	//TODO: "Why is that best practice" - does this need to be called on every access? does not doing this cause a memory leak or something?
	// If this is /not/ required, we could call .WithKeyPrefix twice, once in the main method, (adding prefix) and once here, adding name to the then-passed IDatabase
	private IDatabase Db => redis.GetDatabase().WithKeyPrefix(prefix + name + ":");

	public async Task TickAsync(IServiceScopeFactory scopeFactory, CancellationToken token) {
		var actualParallelism = Math.Min(parallelism - await Db.ListLengthAsync("running"),
		                                 await Db.ListLengthAsync("queued"));
		if (actualParallelism == 0) return;

		var tasks = new List<Task>();
		for (var i = 0; i < actualParallelism; i++) tasks.Add(ProcessJobAsync(scopeFactory, token));

		await Task.WhenAll(tasks);
	}

	public async Task RecoverOrPrepareForExitAsync() {
		while (await Db.ListLengthAsync("running") > 0)
			await Db.ListMoveAsync("running", "queued", ListSide.Right, ListSide.Left);
	}

	private async Task ProcessJobAsync(IServiceScopeFactory scopeFactory, CancellationToken token) {
		var res = await Db.ListMoveAsync("queued", "running", ListSide.Left, ListSide.Right);
		if (res.IsNull || res.Box() is not byte[] buffer) return;
		var job = RedisHelpers.Deserialize<T>(buffer);
		if (job == null) return;
		job.Status    = Job.JobStatus.Running;
		job.StartedAt = DateTime.Now;
		var scope = scopeFactory.CreateScope();
		try {
			await handler(job, scope.ServiceProvider, token);
		}
		catch (Exception e) {
			job.Status           = Job.JobStatus.Failed;
			job.ExceptionMessage = e.Message;
			job.ExceptionSource  = e.TargetSite?.DeclaringType?.FullName ?? "Unknown";

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			if (e is GracefulException { Details: not null } ce) {
				logger.LogError("Failed to process job in {queue} queue: {error} - {details}",
				                name, ce.Message, ce.Details);
			}
			else {
				logger.LogError("Failed to process job in {queue} queue: {error}", name, e.Message);
			}
		}

		if (job.Status is Job.JobStatus.Failed) {
			job.FinishedAt = DateTime.Now;
			await Db.ListRemoveAsync("running", res, 1);
			await Db.ListRightPushAsync("failed", RedisValue.Unbox(RedisHelpers.Serialize(job)));
		}
		else if (job.Status is Job.JobStatus.Delayed) {
			if (job.DelayedUntil == null) {
				job.Status           = Job.JobStatus.Failed;
				job.ExceptionMessage = "Job marked as delayed but no until time set";
				job.ExceptionSource  = typeof(QueueService).FullName;
				job.FinishedAt       = DateTime.Now;
			}
		}
		else {
			job.Status     = Job.JobStatus.Completed;
			job.FinishedAt = DateTime.Now;

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			logger.LogTrace("Job in queue {queue} completed after {duration} ms, was queued for {queueDuration} ms",
			                name, job.Duration, job.QueueDuration);
		}

		var targetQueue = job.Status switch {
			Job.JobStatus.Completed => "completed",
			Job.JobStatus.Failed    => "failed",
			Job.JobStatus.Delayed   => "delayed",
			_                       => throw new Exception("ProcessJob: unknown job state on finish")
		};
		await Db.ListRemoveAsync("running", res, 1);
		if (targetQueue == "delayed") {
			await Db.ListRightPushAsync(targetQueue, RedisValue.Unbox(RedisHelpers.Serialize(job)));
		}
		else {
			await Db.ListLeftPushAsync(targetQueue, RedisValue.Unbox(RedisHelpers.Serialize(job)));
			await Db.ListTrimAsync(targetQueue, 0, 9);
		}

		scope.Dispose();
	}

	public async Task EnqueueAsync(T job) {
		await Db.ListRightPushAsync("queued", RedisValue.Unbox(RedisHelpers.Serialize(job)));
	}
}

[ProtoContract]
[ProtoInclude(100, typeof(InboxJob))]
[ProtoInclude(101, typeof(DeliverJob))]
public abstract class Job {
	public enum JobStatus {
		Queued,
		Delayed,
		Running,
		Completed,
		Failed
	}

	[ProtoMember(4)] public DateTime? DelayedUntil;

	[ProtoMember(10)] public string?   ExceptionMessage;
	[ProtoMember(11)] public string?   ExceptionSource;
	[ProtoMember(3)]  public DateTime? FinishedAt;

	[ProtoMember(1)] public DateTime  QueuedAt = DateTime.Now;
	[ProtoMember(2)] public DateTime? StartedAt;

	public JobStatus Status = JobStatus.Queued;
	public long      Duration      => (long)((FinishedAt ?? DateTime.Now) - (StartedAt ?? QueuedAt)).TotalMilliseconds;
	public long      QueueDuration => (long)((StartedAt ?? DateTime.Now) - QueuedAt).TotalMilliseconds;
}

//TODO: handle delayed jobs
//TODO: retries
//TODO: exponential backoff with fail after certain point
//TODO: prune dead instances after a while (and only resume sending activities after they come back)