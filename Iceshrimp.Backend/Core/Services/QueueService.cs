using Iceshrimp.Backend.Core.Queues;

namespace Iceshrimp.Backend.Core.Services;

public class QueueService(ILogger<QueueService> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService {
	private readonly List<IJobQueue>      _queues      = [];
	public readonly  JobQueue<DeliverJob> DeliverQueue = Queues.DeliverQueue.Create();
	public readonly  JobQueue<InboxJob>   InboxQueue   = Queues.InboxQueue.Create();

	protected override async Task ExecuteAsync(CancellationToken token) {
		_queues.AddRange([InboxQueue, DeliverQueue]);

		while (!token.IsCancellationRequested) {
			foreach (var _ in _queues.Select(queue => queue.Tick(serviceScopeFactory, token))) { }

			await Task.Delay(1000, token);
		}
	}
}

public interface IJobQueue {
	public Task Tick(IServiceScopeFactory scopeFactory, CancellationToken token);
}

public class JobQueue<T>(string name, Func<T, IServiceProvider, CancellationToken, Task> handler, int parallelism)
	: IJobQueue where T : Job {
	private readonly List<T>  _jobs  = [];
	private readonly Queue<T> _queue = new();

	public async Task Tick(IServiceScopeFactory scopeFactory, CancellationToken token) {
		var actualParallelism = parallelism - _jobs.Count(p => p.Status == Job.JobStatus.Running);
		if (actualParallelism == 0) return;

		var tasks = new List<Task>();
		for (var i = 0; i < actualParallelism; i++) tasks.Add(ProcessJob(scopeFactory, token));

		await Task.WhenAll(tasks);
		CleanupFinished();
	}

	private async Task ProcessJob(IServiceScopeFactory scopeFactory, CancellationToken token) {
		if (!_queue.TryDequeue(out var job)) return;
		job.Status = Job.JobStatus.Running;
		var scope = scopeFactory.CreateScope();
		try {
			await handler(job, scope.ServiceProvider, token);
		}
		catch (Exception e) {
			job.Status    = Job.JobStatus.Failed;
			job.Exception = e;

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			logger.LogError("Failed to process job in {queue} queue: {error}", name, e.Message);
		}

		if (job.Status is Job.JobStatus.Failed) {
			job.FinishedAt = DateTime.Now;
		}
		else if (job.Status is Job.JobStatus.Delayed && job.DelayedUntil == null) {
			job.Status     = Job.JobStatus.Failed;
			job.Exception  = new Exception("Job marked as delayed but no until time set");
			job.FinishedAt = DateTime.Now;
		}
		else {
			job.Status     = Job.JobStatus.Completed;
			job.FinishedAt = DateTime.Now;

			var logger = scope.ServiceProvider.GetRequiredService<ILogger<QueueService>>();
			logger.LogTrace("Job in queue {queue} completed after {ms} ms", name, job.Duration);
		}

		scope.Dispose();
	}

	private void CleanupFinished() {
		var count = _jobs.Count(p => p.Status is Job.JobStatus.Completed or Job.JobStatus.Failed);
		if (count <= 100) return;

		//TODO: surely there is a more efficient way to do this
		foreach (var job in _jobs.Where(p => p.Status is Job.JobStatus.Completed or Job.JobStatus.Failed)
		                         .OrderBy(p => p.FinishedAt).Take(count - 50))
			_jobs.Remove(job);
	}

	public void Enqueue(T job) {
		_jobs.Add(job);
		_queue.Enqueue(job);
	}
}

public abstract class Job {
	public enum JobStatus {
		Queued,
		Delayed,
		Running,
		Completed,
		Failed
	}

	public DateTime?  DelayedUntil;
	public Exception? Exception;
	public DateTime?  FinishedAt;
	public DateTime   QueuedAt = DateTime.Now;

	public JobStatus Status = JobStatus.Queued;
	public long      Duration => (long)((FinishedAt ?? DateTime.Now) - QueuedAt).TotalMilliseconds;
}

//TODO: handle delayed jobs
//TODO: retries
//TODO: exponential backoff with fail after certain point
//TODO: prune dead instances after a while (and only resume sending activities after they come back)
//TODO: persistence with redis