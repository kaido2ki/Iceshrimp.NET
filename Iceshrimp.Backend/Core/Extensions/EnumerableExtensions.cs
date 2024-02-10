namespace Iceshrimp.Backend.Core.Extensions;

public static class EnumerableExtensions {
	public static async Task<IEnumerable<T>> AwaitAllAsync<T>(this IEnumerable<Task<T>> tasks) {
		return await Task.WhenAll(tasks);
	}
	
	public static async Task<List<T>> AwaitAllNoConcurrencyAsync<T>(this IEnumerable<Task<T>> tasks) {
		var results = new List<T>();
		foreach (var task in tasks) {
			results.Add(await task);
		}

		return results;
	}
}