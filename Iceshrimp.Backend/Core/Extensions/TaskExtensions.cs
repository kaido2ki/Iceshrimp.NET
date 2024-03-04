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

	public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> task)
	{
		return (await task).ToList();
	}
}