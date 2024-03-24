using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class CronService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken token)
	{
		var tasks = AssemblyHelpers.GetImplementationsOfInterface(typeof(ICronTask))
		                           .Select(p => Activator.CreateInstance(p) as ICronTask)
		                           .Where(p => p != null)
		                           .Cast<ICronTask>();

		foreach (var task in tasks)
		{
			ICronTrigger trigger = task.Type switch
			{
				CronTaskType.Daily    => new DailyTrigger(task.Trigger, token),
				CronTaskType.Interval => new IntervalTrigger(task.Trigger, token),
				_                     => throw new ArgumentOutOfRangeException()
			};

			trigger.OnTrigger += async void () =>
			{
				try
				{
					await using var scope = serviceScopeFactory.CreateAsyncScope();
					await task.Invoke(scope.ServiceProvider);
				}
				catch
				{
					// ignored (errors in the event handler crash the host process)
				}
			};
		}

		return Task.CompletedTask;
	}
}

public interface ICronTask
{
	public TimeSpan     Trigger { get; }
	public CronTaskType Type    { get; }
	public Task         Invoke(IServiceProvider provider);
}

public enum CronTaskType
{
	Daily,
	Interval
}

public interface ICronTrigger
{
	public event Action? OnTrigger;
}

file class DailyTrigger : ICronTrigger, IDisposable
{
	public DailyTrigger(TimeSpan triggerTime, CancellationToken cancellationToken)
	{
		TriggerTime       = triggerTime;
		CancellationToken = cancellationToken;

		RunningTask = Task.Run(async () =>
		{
			while (!CancellationToken.IsCancellationRequested)
			{
				var nextTrigger = DateTime.Today + TriggerTime - DateTime.Now;
				if (nextTrigger < TimeSpan.Zero)
					nextTrigger = nextTrigger.Add(new TimeSpan(24, 0, 0));
				await Task.Delay(nextTrigger, CancellationToken);
				OnTrigger?.Invoke();
			}
		}, CancellationToken);
	}

	private TimeSpan          TriggerTime       { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public event Action? OnTrigger;

	public void Dispose()
	{
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}

	~DailyTrigger() => Dispose();
}

file class IntervalTrigger : ICronTrigger, IDisposable
{
	public IntervalTrigger(TimeSpan triggerInterval, CancellationToken cancellationToken)
	{
		TriggerInterval   = triggerInterval;
		CancellationToken = cancellationToken;

		RunningTask = Task.Run(async () =>
		{
			while (!CancellationToken.IsCancellationRequested)
			{
				await Task.Delay(TriggerInterval, CancellationToken);
				OnTrigger?.Invoke();
			}
		}, CancellationToken);
	}

	private TimeSpan          TriggerInterval   { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public event Action? OnTrigger;

	public void Dispose()
	{
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}

	~IntervalTrigger() => Dispose();
}