namespace Iceshrimp.Backend.Core.Helpers;

public sealed class AsyncAutoResetEvent(bool signaled = false)
{
	private readonly List<TaskCompletionSource<bool>> _taskCompletionSources        = [];
	private readonly List<TaskCompletionSource<bool>> _noResetTaskCompletionSources = [];
	public           bool                             Signaled => signaled;

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

	public Task<bool> WaitWithoutResetAsync(CancellationToken cancellationToken = default)
	{
		lock (_taskCompletionSources)
		{
			if (signaled)
				return Task.FromResult(true);

			var tcs = new TaskCompletionSource<bool>();
			cancellationToken.Register(Callback, (this, tcs));
			_noResetTaskCompletionSources.Add(tcs);
			return tcs.Task;
		}
	}

	public void Set()
	{
		lock (_taskCompletionSources)
		{
			signaled = true;
			foreach (var tcs in _noResetTaskCompletionSources.ToList())
			{
				_noResetTaskCompletionSources.Remove(tcs);
				tcs.TrySetResult(true);
			}

			if (_taskCompletionSources.Count == 0) return;
			
			signaled = false;
			foreach (var tcs in _taskCompletionSources.ToList())
			{
				_taskCompletionSources.Remove(tcs);
				tcs.TrySetResult(true);
			}
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
			ev._noResetTaskCompletionSources.Remove(tcs);
		}
	}
}