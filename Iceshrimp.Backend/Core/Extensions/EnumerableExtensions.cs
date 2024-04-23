namespace Iceshrimp.Backend.Core.Extensions;

public static class EnumerableExtensions
{
	public static async Task<IEnumerable<T>> AwaitAllAsync<T>(this IEnumerable<Task<T>> tasks)
	{
		return await Task.WhenAll(tasks);
	}

	public static async Task AwaitAllAsync(this IEnumerable<Task> tasks)
	{
		await Task.WhenAll(tasks);
	}

	public static async Task<List<T>> AwaitAllNoConcurrencyAsync<T>(this IEnumerable<Task<T>> tasks)
	{
		var results = new List<T>();

		foreach (var task in tasks)
			results.Add(await task);

		return results;
	}

	public static async Task AwaitAllNoConcurrencyAsync(this IEnumerable<Task> tasks)
	{
		foreach (var task in tasks) await task;
	}

	public static bool IsDisjoint<T>(this IEnumerable<T> x, IEnumerable<T> y)
	{
		return x.All(item => !y.Contains(item));
	}

	public static bool Intersects<T>(this IEnumerable<T> x, IEnumerable<T> y)
	{
		return x.Any(y.Contains);
	}
}