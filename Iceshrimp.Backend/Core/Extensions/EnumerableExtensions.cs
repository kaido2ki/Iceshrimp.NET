using EntityFrameworkCore.Projectables;

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

	public static async Task<List<T>> AwaitAllNoConcurrencyAsync<T>(this IAsyncEnumerable<Task<T>> tasks)
	{
		var results = new List<T>();

		await foreach (var task in tasks)
			results.Add(await task);

		return results;
	}

	public static async Task AwaitAllNoConcurrencyAsync(this IEnumerable<Task> tasks)
	{
		foreach (var task in tasks) await task;
	}

	[Projectable]
	public static bool IsDisjoint<T>(this IEnumerable<T> x, IEnumerable<T> y) => x.All(item => !y.Contains(item));

	[Projectable]
	public static bool Intersects<T>(this IEnumerable<T> x, IEnumerable<T> y) => x.Any(y.Contains);

	public static bool IsEquivalent<T>(this IEnumerable<T> x, IEnumerable<T> y)
	{
		var xArray = x as T[] ?? x.ToArray();
		var yArray = y as T[] ?? y.ToArray();
		return xArray.Length == yArray.Length && xArray.All(yArray.Contains);
	}

	public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> @enum) => @enum.OfType<T>();

	public static IEnumerable<T> StructNotNull<T>(this IEnumerable<T?> @enum) where T : struct =>
		@enum.Where(p => p.HasValue).Select(p => p!.Value);
}