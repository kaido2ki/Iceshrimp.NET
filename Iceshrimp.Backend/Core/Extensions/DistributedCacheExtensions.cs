using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Extensions;

public static class DistributedCacheExtensions {
	//TODO: named caches, CacheService? (that optionally uses StackExchange.Redis directly)?
	//TODO: thread-safe locks to prevent fetching data more than once
	//TODO: sliding window ttl?
	//TODO: renew option on GetAsync and FetchAsync
	//TODO: check that this actually works for complex types (sigh)

	public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key) {
		var buffer = await cache.GetAsync(key);
		if (buffer == null || buffer.Length == 0) return default;

		var stream = new MemoryStream(buffer);
		try {
			var data = await JsonSerializer.DeserializeAsync<T>(stream);
			return data != null ? (T)data : default;
		}
		catch {
			return default;
		}
	}

	public static async Task<T> FetchAsync<T>(this IDistributedCache cache, string key, TimeSpan ttl,
	                                          Func<Task<T>> fetcher) {
		var hit = await cache.GetAsync<T>(key);
		if (hit != null) return hit;

		var fetched = await fetcher();
		await cache.SetAsync(key, fetched, ttl);
		return fetched;
	}

	public static async Task SetAsync<T>(this IDistributedCache cache, string key, T data, TimeSpan ttl) {
		using var stream = new MemoryStream();
		await JsonSerializer.SerializeAsync(stream, data);
		stream.Position = 0;
		var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
		await cache.SetAsync(key, stream.ToArray(), options);
	}
}