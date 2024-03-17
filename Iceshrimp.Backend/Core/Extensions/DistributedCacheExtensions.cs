using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Extensions;

public static class DistributedCacheExtensions
{
	//TODO: named caches, CacheService? (that optionally uses StackExchange.Redis directly)?
	//TODO: thread-safe locks to prevent fetching data more than once
	//TODO: check that this actually works for complex types (sigh)

	private static readonly JsonSerializerOptions Options =
		new(JsonSerializerOptions.Default) { ReferenceHandler = ReferenceHandler.Preserve };

	public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key, bool renew = false)
		where T : class?
	{
		var buffer = await cache.GetAsync(key);
		if (buffer == null || buffer.Length == 0) return null;

		if (renew)
			await cache.RefreshAsync(key);

		var stream = new MemoryStream(buffer);
		try
		{
			var data = await JsonSerializer.DeserializeAsync<T?>(stream, Options);
			return data;
		}
		catch
		{
			return null;
		}
	}

	public static async Task<T?> GetAsyncValue<T>(this IDistributedCache cache, string key, bool renew = false)
		where T : struct
	{
		var buffer = await cache.GetAsync(key);
		if (buffer == null || buffer.Length == 0) return null;

		if (renew)
			await cache.RefreshAsync(key);

		var stream = new MemoryStream(buffer);
		try
		{
			var data = await JsonSerializer.DeserializeAsync<T?>(stream, Options);
			return data;
		}
		catch
		{
			return null;
		}
	}

	public static async Task<T> FetchAsync<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T>> fetcher, bool renew = false
	) where T : class?
	{
		var hit = await cache.GetAsync<T>(key, renew);
		if (hit != null) return hit;

		var fetched = await fetcher();
		await cache.SetAsync(key, fetched, ttl, renew);
		return fetched;
	}

	public static async Task<T> FetchAsync<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<T> fetcher, bool renew = false
	) where T : class
	{
		return await FetchAsync(cache, key, ttl, () => Task.FromResult(fetcher()), renew);
	}

	public static async Task<T> FetchAsyncValue<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<T>> fetcher, bool renew = false
	) where T : struct
	{
		var hit = await cache.GetAsyncValue<T>(key, renew);
		if (hit.HasValue) return hit.Value;

		var fetched = await fetcher();
		await cache.SetAsync(key, fetched, ttl, renew);
		return fetched;
	}

	public static async Task<T> FetchAsyncValue<T>(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<T> fetcher, bool renew = false
	) where T : struct
	{
		return await FetchAsyncValue(cache, key, ttl, () => Task.FromResult(fetcher()), renew);
	}

	public static async Task SetAsync<T>(
		this IDistributedCache cache, string key, T data, TimeSpan ttl, bool sliding = false
	)
	{
		using var stream = new MemoryStream();
		await JsonSerializer.SerializeAsync(stream, data, Options);
		stream.Position = 0;
		var options = sliding
			? new DistributedCacheEntryOptions { SlidingExpiration               = ttl }
			: new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
		await cache.SetAsync(key, stream.ToArray(), options);
	}

	public static async Task CacheAsync(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<Task<object?>> fetcher, Type type,
		bool renew = false
	)
	{
		var res = await cache.GetAsync(key);
		if (res != null) return;
		await SetAsync(cache, key, await fetcher(), type, ttl, renew);
	}

	public static async Task CacheAsync(
		this IDistributedCache cache, string key, TimeSpan ttl, Func<object?> fetcher, Type type, bool renew = false
	)
	{
		var res = await cache.GetAsync(key);
		if (res != null) return;
		await SetAsync(cache, key, fetcher(), type, ttl, renew);
	}

	public static async Task CacheAsync(
		this IDistributedCache cache, string key, TimeSpan ttl, object? value, Type type, bool renew = false
	)
	{
		await CacheAsync(cache, key, ttl, () => value, type, renew);
	}

	private static async Task SetAsync(
		this IDistributedCache cache, string key, object? data, Type type, TimeSpan ttl, bool sliding = false
	)
	{
		using var stream = new MemoryStream();
		await JsonSerializer.SerializeAsync(stream, data, type, Options);
		stream.Position = 0;
		var options = sliding
			? new DistributedCacheEntryOptions { SlidingExpiration               = ttl }
			: new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
		await cache.SetAsync(key, stream.ToArray(), options);
	}
}