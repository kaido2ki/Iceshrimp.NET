namespace Iceshrimp.Backend.Core.Helpers;

public sealed class AsyncAutoResetEvent(bool signaled = false)
{
	private readonly List<TaskCompletionSource<bool>> _taskCompletionSources = [];

	public Task<bool> WaitAsync(CancellationToken cancellationToken = default)
	{
		lock (_taskCompletionSources)
		{
			if (signaled)
			{
				signaled = false;
				return Task.FromResult(true);
			}

			var tcs = new TaskCompletionSource<bool>();
			cancellationToken.Register(Callback, (this, tcs));
			_taskCompletionSources.Add(tcs);
			return tcs.Task;
		}
	}

	public void Set()
	{
		lock (_taskCompletionSources)
		{
			if (_taskCompletionSources.Count > 0)
			{
				var tcs = _taskCompletionSources[0];
				_taskCompletionSources.RemoveAt(0);
				tcs.TrySetResult(true);
				return;
			}

			signaled = true;
		}
	}

	private static void Callback(object? state)
	{
		var (ev, tcs) = ((AsyncAutoResetEvent, TaskCompletionSource<bool>))state!;
		lock (ev._taskCompletionSources)
		{
			if (tcs.Task.IsCompleted) return;
			tcs.TrySetCanceled();
			ev._taskCompletionSources.Remove(tcs);
		}
	}
}