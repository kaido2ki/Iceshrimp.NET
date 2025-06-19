using System.Reflection;
using Iceshrimp.AssemblyUtils;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class CronService(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
	public CronTaskState[] Tasks { get; private set; } = [];

	public async Task RunCronTaskAsync(ICronTask task, ICronTrigger trigger)
	{
		if (trigger.IsRunning) return;
		trigger.IsRunning = true;
		trigger.Error     = null;

		try
		{
			await using var scope = serviceScopeFactory.CreateAsyncScope();
			await task.InvokeAsync(scope.ServiceProvider);
		}
		catch (Exception e)
		{
			trigger.Error = e;
		}

		trigger.LastRun   = DateTime.UtcNow;
		trigger.IsRunning = false;
	}

	protected override Task ExecuteAsync(CancellationToken token)
	{
		var tasks = PluginLoader.Assemblies
		                        .Prepend(Assembly.GetExecutingAssembly())
		                        .SelectMany(AssemblyLoader.GetImplementationsOfInterface<ICronTask>)
		                        .OrderBy(p => p.AssemblyQualifiedName)
		                        .ThenBy(p => p.Name)
		                        .Select(p => Activator.CreateInstance(p) as ICronTask)
		                        .Where(p => p != null)
		                        .Cast<ICronTask>()
		                        .ToArray();

		List<CronTaskState> stateObjs = [];

		foreach (var task in tasks)
		{
			ICronTrigger trigger = task.Type switch
			{
				CronTaskType.Daily    => new DailyTrigger(task.Trigger, token),
				CronTaskType.Interval => new IntervalTrigger(task.Trigger, token),
				_                     => throw new ArgumentOutOfRangeException()
			};

			trigger.OnTrigger += async void (state) =>
			{
				try
				{
					await RunCronTaskAsync(task, state);
				}
				catch
				{
					// ignored (errors in the event handler crash the host process)
				}
			};

			stateObjs.Add(new CronTaskState { Task = task, Trigger = trigger });
		}

		Tasks = stateObjs.ToArray();

		return Task.CompletedTask;
	}
}

public class CronTaskState
{
	public required ICronTask    Task;
	public required ICronTrigger Trigger;
}

public interface ICronTask
{
	public TimeSpan     Trigger { get; }
	public CronTaskType Type    { get; }
	public Task         InvokeAsync(IServiceProvider provider);
}

public enum CronTaskType
{
	Daily,
	Interval
}

public interface ICronTrigger
{
	public event Action<ICronTrigger>? OnTrigger;

	public DateTime   NextTrigger { get; }
	public DateTime?  LastRun     { get; set; }
	public bool       IsRunning   { get; set; }
	public Exception? Error       { get; set; }

	public TimeSpan UpdateNextTrigger();
}

public class DailyTrigger : ICronTrigger, IDisposable
{
	public DailyTrigger(TimeSpan triggerTime, CancellationToken cancellationToken)
	{
		TriggerTime       = triggerTime;
		CancellationToken = cancellationToken;
		NextTrigger       = DateTime.UtcNow;

		RunningTask = Task.Run(async () =>
		{
			while (!CancellationToken.IsCancellationRequested)
			{
				var nextTrigger = UpdateNextTrigger();
				await Task.Delay(nextTrigger, CancellationToken);
				OnTrigger?.Invoke(this);
			}
		}, CancellationToken);
	}

	public TimeSpan UpdateNextTrigger()
	{
		var nextTrigger = DateTime.Today + TriggerTime - DateTime.Now;
		if (nextTrigger < TimeSpan.Zero)
			nextTrigger = nextTrigger.Add(new TimeSpan(24, 0, 0));

		NextTrigger = DateTime.UtcNow + nextTrigger;

		return nextTrigger;
	}

	private TimeSpan          TriggerTime       { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public event Action<ICronTrigger>? OnTrigger;

	public DateTime   NextTrigger { get; set; }
	public DateTime?  LastRun     { get; set; }
	public bool       IsRunning   { get; set; }

	public Exception? Error { get; set; } = null;

	public void Dispose()
	{
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}
}

public class IntervalTrigger : ICronTrigger, IDisposable
{
	public IntervalTrigger(TimeSpan triggerInterval, CancellationToken cancellationToken)
	{
		TriggerInterval   = triggerInterval;
		CancellationToken = cancellationToken;
		NextTrigger       = DateTime.UtcNow + TriggerInterval;

		RunningTask = Task.Run(async () =>
		{
			while (!CancellationToken.IsCancellationRequested)
			{
				UpdateNextTrigger();
				await Task.Delay(TriggerInterval, CancellationToken);

				while (LastRun != null && LastRun + TriggerInterval + TimeSpan.FromMinutes(5) < DateTime.UtcNow)
				{
					NextTrigger = LastRun.Value + TriggerInterval;
					await Task.Delay(NextTrigger - DateTime.UtcNow);
				}

				OnTrigger?.Invoke(this);
			}
		}, CancellationToken);
	}

	public TimeSpan UpdateNextTrigger()
	{
		NextTrigger = DateTime.UtcNow + TriggerInterval;
		return TriggerInterval;
	}

	private TimeSpan          TriggerInterval   { get; }
	private CancellationToken CancellationToken { get; }
	private Task              RunningTask       { get; set; }

	public event Action<ICronTrigger>? OnTrigger;

	public DateTime  NextTrigger { get; set; }
	public DateTime? LastRun     { get; set; }
	public bool      IsRunning   { get; set; }

	public Exception? Error { get; set; } = null;

	public void Dispose()
	{
		RunningTask.Dispose();
		RunningTask = null!;
		GC.SuppressFinalize(this);
	}
}
