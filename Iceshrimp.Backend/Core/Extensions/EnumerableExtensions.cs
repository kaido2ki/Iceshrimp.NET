namespace Iceshrimp.Backend.Core.Extensions;

public static class EnumerableExtensions {
	public static async Task<IEnumerable<T>> AwaitAllAsync<T>(this IEnumerable<Task<T>> tasks) {
		return await Task.WhenAll(tasks);
	}
}