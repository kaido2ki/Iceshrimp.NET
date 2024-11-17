using System.Text.Json;
using System.Text.Json.Serialization;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class CacheService([FromKeyedServices("cache")] DatabaseContext db) : IScopedService
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	private static readonly JsonSerializerOptions Options =
		new(JsonSerializerOptions.Default) { ReferenceHandler = ReferenceHandler.Preserve };

	public async Task<T?> GetAsync<T>(string key, bool renew = false) where T : class?
	{
		var res = await GetValueAsync(key);
		if (res == null) return null;

		if (renew)
			await RenewAsync(key);

		try
		{
			return JsonSerializer.Deserialize<T?>(res, Options);
		}
		catch
		{
			return null;
		}
	}

	public async Task<T?> GetValueAsync<T>(string key, bool renew = false) where T : struct
	{
		var res = await GetValueAsync(key);
		if (res == null) return null;

		if (renew)
			await RenewAsync(key);

		try
		{
			return JsonSerializer.Deserialize<T?>(res, Options);
		}
		catch
		{
			return null;
		}
	}

	public async Task SetAsync<T>(string key, T data, TimeSpan ttl) where T : class?
	{
		var json = JsonSerializer.Serialize(data, Options);
		await SetValueAsync(key, json, ttl);
	}

	public async Task SetValueAsync<T>(string key, T data, TimeSpan ttl) where T : struct
	{
		var json = JsonSerializer.Serialize(data, Options);
		await SetValueAsync(key, json, ttl);
	}

	public async Task<T> FetchAsync<T>(
		string key, TimeSpan ttl, Func<Task<T>> fetcher, bool renew = false
	) where T : class?
	{
		var hit = await GetAsync<T>(key, renew);
		if (hit != null) return hit;

		var refetch = KeyedLocker.IsInUse(key);

		using (await KeyedLocker.LockAsync(key))
		{
			if (refetch)
			{
				hit = await GetAsync<T>(key, renew);
				if (hit != null) return hit;
			}

			var fetched = await fetcher();
			await SetAsync(key, fetched, ttl);
			return fetched;
		}
	}

	public async Task<T> FetchAsync<T>(string key, TimeSpan ttl, Func<T> fetcher, bool renew = false)
		where T : class? => await FetchAsync(key, ttl, () => Task.FromResult(fetcher()), renew);

	public async Task<T> FetchValueAsync<T>(
		string key, TimeSpan ttl, Func<Task<T>> fetcher, bool renew = false
	) where T : struct
	{
		var hit = await GetValueAsync<T>(key, renew);
		if (hit.HasValue) return hit.Value;

		var refetch = KeyedLocker.IsInUse(key);

		using (await KeyedLocker.LockAsync(key))
		{
			if (refetch)
			{
				hit = await GetValueAsync<T>(key, renew);
				if (hit.HasValue) return hit.Value;
			}

			var fetched = await fetcher();
			await SetValueAsync(key, fetched, ttl);
			return fetched;
		}
	}

	public async Task<T> FetchValueAsync<T>(string key, TimeSpan ttl, Func<T> fetcher, bool renew = false)
		where T : struct => await FetchValueAsync(key, ttl, () => Task.FromResult(fetcher()), renew);

	public async Task ClearAsync(string key) => await db.CacheStore.Where(p => p.Key == key).ExecuteDeleteAsync();

	private async Task<string?> GetValueAsync(string key)
	{
		return await db.CacheStore
		               .Where(p => p.Key == key && (p.Expiry == null || p.Expiry > DateTime.UtcNow))
		               .Select(p => p.Value)
		               .FirstOrDefaultAsync();
	}

	private async Task SetValueAsync(string key, string? value, TimeSpan? ttl)
	{
		var expiry = ttl != null ? DateTime.UtcNow + ttl : null;
		var entity = await db.CacheStore.FirstOrDefaultAsync(p => p.Key == key);
		if (entity != null)
		{
			entity.Value  = value;
			entity.Expiry = expiry;
			entity.Ttl    = ttl;
			await db.SaveChangesAsync();
		}
		else
		{
			entity = new CacheEntry
			{
				Key    = key,
				Value  = value,
				Expiry = expiry,
				Ttl    = ttl
			};

			await db.CacheStore.Upsert(entity)
			        .On(p => p.Key)
			        .WhenMatched((_, orig) => new CacheEntry
			        {
				        Value  = orig.Value,
				        Expiry = orig.Expiry,
				        Ttl    = orig.Ttl
			        })
			        .RunAsync();
		}
	}

	private async Task RenewAsync(string key)
	{
		await db.CacheStore
		        .Where(p => p.Key == key && p.Expiry != null && p.Expiry > DateTime.UtcNow && p.Ttl != null)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.Expiry, i => i.Expiry + i.Ttl));
	}
}