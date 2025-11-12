using System.Diagnostics.CodeAnalysis;

namespace Iceshrimp.Backend.Core.Extensions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class TaskExtensions
{
	extension(Task task)
	{
		public async Task SafeWaitAsync(TimeSpan timeSpan)
		{
			try
			{
				await task.WaitAsync(timeSpan);
			}
			catch (TimeoutException)
			{
				// ignored
			}
		}

		public async Task SafeWaitAsync(CancellationToken token)
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

		public async Task SafeWaitAsync(CancellationToken token, Action action)
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

		public async Task SafeWaitAsync(CancellationToken token, Func<Task> action)
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
	}

	extension(Func<Task> factory)
	{
		public List<Task> QueueMany(int n) =>
			Enumerable.Range(0, n).Select(_ => factory()).ToList();
	}

	extension<T>(Task<IEnumerable<T>> task)
	{
		public async Task<List<T>> ToListAsync()
		{
			return (await task).ToList();
		}

		public async Task<T[]> ToArrayAsync()
		{
			return (await task).ToArray();
		}

		public async Task<T?> FirstOrDefaultAsync()
		{
			return (await task).FirstOrDefault();
		}
	}

	extension(Task task)
	{
		public async Task ContinueWithResult(Action continuation)
		{
			await task;
			continuation();
		}

		public async Task<TNewResult> ContinueWithResult<TNewResult>(
			Func<TNewResult> continuation
		)
		{
			await task;
			return continuation();
		}
	}

	extension<TResult>(Task<TResult> task)
	{
		public async Task ContinueWithResult(Action<TResult> continuation)
		{
			continuation(await task);
		}

		public async Task<TNewResult> ContinueWithResult<TNewResult>(
			Func<TResult, TNewResult> continuation
		)
		{
			return continuation(await task);
		}
	}

	extension(Task task)
	{
		public async Task ContinueWithResult(Func<Task> continuation)
		{
			await task;
			await continuation();
		}

		public async Task<TNewResult> ContinueWithResult<TNewResult>(
			Func<Task<TNewResult>> continuation
		)
		{
			await task;
			return await continuation();
		}
	}

	extension<TResult>(Task<TResult> task)
	{
		public async Task ContinueWithResult(Func<TResult, Task> continuation)
		{
			await continuation(await task);
		}

		public async Task<TNewResult> ContinueWithResult<TNewResult>(
			Func<TResult, Task<TNewResult>> continuation
		)
		{
			return await continuation(await task);
		}
	}
}