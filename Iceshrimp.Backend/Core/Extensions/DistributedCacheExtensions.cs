using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Extensions;

public static class DistributedCacheExtensions {
	//TODO: named caches, CacheService? (that optionally uses StackExchange.Redis directly)?
	//TODO: thread-safe locks to prevent fetching data more than once
	//TODO: sliding window ttl?
	//TODO: renew option on GetAsync and FetchAsync
	//TODO: check that this actually works for complex types (sigh)

	public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key) where T : class {
		var buffer = await cache.GetAsync(key);
		if (buffer == null || buffer.Length == 0) return null;

		var stream = new MemoryStream(buffer);
		try {
			var data = await JsonSerializer.DeserializeAsync<T?>(stream);
			return data;
		}
		catch {
			return null;
		}
	}

	public static async Task<T?> GetAsyncValue<T>(this IDistributedCache cache, string key) where T : struct {
		var buffer = await cache.GetAsync(key);
		if (buffer == null || buffer.Length == 0) return null;

		var stream = new MemoryStream(buffer);
		try {
			var data = await JsonSerializer.DeserializeAsync<T?>(stream);
			return data;
		}
		catch {
			return null;
		}
	}

	public static async Task<T> FetchAsync<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T>> fetcher
	) where T : class {
		var hit = await cache.GetAsync<T>(key);
		if (hit != null) return hit;

		var fetched = await fetcher();
		await cache.SetAsync(key, fetched, ttl);
		return fetched;
	}

	public static async Task<T> FetchAsync<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<T> fetcher
	) where T : class {
		return await FetchAsync(cache, key, ttl, () => Task.FromResult(fetcher()));
	}

	public static async Task<T> FetchAsyncValue<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T>> fetcher
	) where T : struct {
		var hit = await cache.GetAsyncValue<T>(key);
		if (hit.HasValue) return hit.Value;

		var fetched = await fetcher();
		await cache.SetAsync(key, fetched, ttl);
		return fetched;
	}
	
	public static async Task<T> FetchAsyncValue<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<T> fetcher
	) where T : struct {
		return await FetchAsyncValue(cache, key, ttl, () => Task.FromResult(fetcher()));
	}

	public static async Task SetAsync<T>(this IDistributedCache cache, string key, T data, TimeSpan ttl) {
		using var stream = new MemoryStream();
		await JsonSerializer.SerializeAsync(stream, data);
		stream.Position = 0;
		var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
		await cache.SetAsync(key, stream.ToArray(), options);
	}
}