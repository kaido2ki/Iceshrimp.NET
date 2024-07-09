namespace Iceshrimp.Backend.Core.Extensions;

public static class TaskExtensions
{
	public static async Task SafeWaitAsync(this Task task, TimeSpan timeSpan)
	{
		try
		{
			await task.WaitAsync(TimeSpan.FromMilliseconds(500));
		}
		catch (TimeoutException)
		{
			// ignored
		}
	}

	public static async Task SafeWaitAsync(this Task task, CancellationToken token)
	{
		try
		{
			await task.WaitAsync(token);
		}
		catch (TaskCanceledException)
		{
			// ignored
		}
	}

	public static async Task SafeWaitAsync(this Task task, CancellationToken token, Action action)
	{
		try
		{
			await task.WaitAsync(token);
		}
		catch (TaskCanceledException)
		{
			action();
		}
	}

	public static async Task SafeWaitAsync(this Task task, CancellationToken token, Func<Task> action)
	{
		try
		{
			await task.WaitAsync(token);
		}
		catch (TaskCanceledException)
		{
			await action();
		}
	}

	public static List<Task> QueueMany(this Func<Task> task, int n)
	{
		var tasks = new List<Task>();
		for (var i = 0; i < n; i++)
			tasks.Add(task());
		return tasks;
	}

	public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> task)
	{
		return (await task).ToList();
	}

	public static async Task ContinueWithResult(this Task task, Action continuation)
	{
		await task;
		continuation();
	}

	public static async Task<TNewResult> ContinueWithResult<TNewResult>(this Task task, Func<TNewResult> continuation)
	{
		await task;
		return continuation();
	}

	public static async Task ContinueWithResult<TResult>(this Task<TResult> task, Action<TResult> continuation)
	{
		continuation(await task);
	}

	public static async Task<TNewResult> ContinueWithResult<TResult, TNewResult>(
		this Task<TResult> task, Func<TResult, TNewResult> continuation
	)
	{
		return continuation(await task);
	}

	public static async Task ContinueWithResult(this Task task, Func<Task> continuation)
	{
		await task;
		await continuation();
	}

	public static async Task<TNewResult> ContinueWithResult<TNewResult>(
		this Task task, Func<Task<TNewResult>> continuation
	)
	{
		await task;
		return await continuation();
	}

	public static async Task ContinueWithResult<TResult>(this Task<TResult> task, Func<TResult, Task> continuation)
	{
		await continuation(await task);
	}

	public static async Task<TNewResult> ContinueWithResult<TResult, TNewResult>(
		this Task<TResult> task, Func<TResult, Task<TNewResult>> continuation
	)
	{
		return await continuation(await task);
	}
}