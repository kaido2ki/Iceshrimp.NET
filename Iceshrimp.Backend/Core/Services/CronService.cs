using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class CronService(IServiceScopeFactory serviceScopeFactory) : BackgroundService {
	protected override Task ExecuteAsync(CancellationToken token) {
		var tasks = AssemblyHelpers.GetImplementationsOfInterface(typeof(ICronTask))
		                           .Select(p => Activator.CreateInstance(p) as ICronTask)
		                           .Where(p => p != null)
		                           .Cast<ICronTask>();

		foreach (var task in tasks) {
			ICronTrigger trigger = task.Type switch {
				CronTaskType.Daily    => new DailyTrigger(task.Trigger, token),
				CronTaskType.Interval => new IntervalTrigger(task.Trigger, token),
				_                     => throw new ArgumentOutOfRangeException()
			};

			trigger.OnTrigger += async () => await task.Invoke(serviceScopeFactory.CreateScope().ServiceProvider);
		}

		return Task.CompletedTask;
	}
}

public interface ICronTask {
	public Task Invoke(IServiceProvider provider);

	public TimeSpan     Trigger { get; }
	public CronTaskType Type    { get; }
}

public enum CronTaskType {
	Daily,
	Interval
}

public interface ICronTrigger {
	public event Action? OnTrigger;
}

file class DailyTrigger : ICronTrigger, IDisposable {
	private TimeSpan          TriggerTime       { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public DailyTrigger(TimeSpan triggerTime, CancellationToken cancellationToken) {
		TriggerTime       = triggerTime;
		CancellationToken = cancellationToken;

		RunningTask = Task.Run(async () => {
			while (!CancellationToken.IsCancellationRequested) {
				var nextTrigger = DateTime.Today + TriggerTime - DateTime.Now;
				if (nextTrigger < TimeSpan.Zero)
					nextTrigger = nextTrigger.Add(new TimeSpan(24, 0, 0));
				await Task.Delay(nextTrigger, CancellationToken);
				OnTrigger?.Invoke();
			}
		}, CancellationToken);
	}

	public void Dispose() {
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}

	public event Action? OnTrigger;
	~DailyTrigger() => Dispose();
}

file class IntervalTrigger : ICronTrigger, IDisposable {
	private TimeSpan          TriggerInterval   { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public IntervalTrigger(TimeSpan triggerInterval, CancellationToken cancellationToken) {
		TriggerInterval   = triggerInterval;
		CancellationToken = cancellationToken;

		RunningTask = Task.Run(async () => {
			while (!CancellationToken.IsCancellationRequested) {
				await Task.Delay(TriggerInterval, CancellationToken);
				OnTrigger?.Invoke();
			}
		}, CancellationToken);
	}

	public void Dispose() {
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}

	public event Action? OnTrigger;
	~IntervalTrigger() => Dispose();
}