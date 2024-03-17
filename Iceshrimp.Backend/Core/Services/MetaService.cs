using System.Reflection;
using EntityFramework.Exceptions.Common;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Services;

public class MetaService(IServiceScopeFactory scopeFactory, IDistributedCache cache)
{
	public async Task<T> Get<T>(Meta<T> meta) where T : class? =>
		await cache.FetchAsync($"meta:{meta.Key}", meta.Ttl, async () => await Fetch(meta), true);

	public async Task<T> GetValue<T>(Meta<T> meta) where T : struct =>
		await cache.FetchAsyncValue($"meta:{meta.Key}", meta.Ttl, async () => await Fetch(meta), true);

	public async Task EnsureSet<T>(Meta<T> meta, T value) => await EnsureSet(meta, () => value);

	public async Task EnsureSet<T>(Meta<T> meta, Func<T> value)
	{
		if (await Fetch(meta.Key) != null) return;
		await Set(meta, value());
	}

	public async Task EnsureSet<T>(Meta<T> meta, Func<Task<T>> value)
	{
		if (await Fetch(meta.Key) != null) return;
		await Set(meta, await value());
	}

	public async Task EnsureSet<T>(IReadOnlyList<Meta<T>> metas, Func<List<T>> values)
	{
		if (await GetDbContext().MetaStore.CountAsync(p => metas.Select(m => m.Key).Contains(p.Key)) == metas.Count)
			return;

		var resolvedValues = values();
		if (resolvedValues.Count != metas.Count)
			throw new Exception("Metas count doesn't match values count");

		for (var i = 0; i < metas.Count; i++)
			await Set(metas[i], resolvedValues[i]);
	}

	public async Task Set<T>(Meta<T> meta, T value) => await Set(meta.Key, meta.ConvertSet(value), meta.Ttl);

	public async Task WarmupCache()
	{
		var entities =
			typeof(MetaEntity)
				.GetMembers(BindingFlags.Static | BindingFlags.Public)
				.OfType<FieldInfo>()
				.Where(p => p.FieldType.IsAssignableTo(typeof(Meta)))
				.Select(p => p.GetValue(this))
				.Cast<Meta>();

		var store = await GetDbContext().MetaStore.ToListAsync();
		var dict = entities.ToDictionary(p => p, p => p.ConvertCache(store.FirstOrDefault(i => i.Key == p.Key)?.Value));
		var invalid = dict.Where(p => !p.Key.IsNullable && p.Value == null).Select(p => p.Key.Key).ToList();
		if (invalid.Count != 0)
			throw new Exception($"Invalid meta store entries: [{string.Join(", ", invalid)}] must not be null");

		foreach (var entry in dict)
			await Cache(entry.Key, entry.Value);
	}

	private async Task Cache(Meta meta, object? value) =>
		await cache.CacheAsync($"meta:{meta.Key}", meta.Ttl, value, meta.Type, true);

	private async Task<T> Fetch<T>(Meta<T> meta) => meta.ConvertGet(await Fetch(meta.Key));

	private async Task<string?> Fetch(string key) =>
		await GetDbContext().MetaStore.Where(p => p.Key == key).Select(p => p.Value).FirstOrDefaultAsync();

	private async Task Set(string key, string? value, TimeSpan ttl)
	{
		var db     = GetDbContext();
		var entity = await db.MetaStore.FirstOrDefaultAsync(p => p.Key == key);
		if (entity != null)
		{
			entity.Value = value;
			await db.SaveChangesAsync();
		}
		else
		{
			entity = new MetaStoreEntry { Key = key, Value = value };
			db.Add(entity);
			try
			{
				await db.SaveChangesAsync();
			}
			catch (UniqueConstraintException)
			{
				db.Remove(entity);
				entity = await db.MetaStore.FirstOrDefaultAsync(p => p.Key == key) ??
				         throw new Exception("Failed to fetch entity after UniqueConstraintException");
				entity.Value = value;
				await db.SaveChangesAsync();
			}
		}

		await cache.SetAsync($"meta:{key}", value, ttl, true);
	}

	private DatabaseContext GetDbContext() =>
		scopeFactory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
}

public static class MetaEntity
{
	public static readonly StringMeta         VapidPrivateKey     = new("vapid_private_key");
	public static readonly StringMeta         VapidPublicKey      = new("vapid_public_key");
	public static readonly NullableStringMeta InstanceName        = new("instance_name");
	public static readonly NullableStringMeta InstanceDescription = new("instance_description");
	public static readonly NullableStringMeta AdminContactEmail   = new("admin_contact_email");
}

public class Meta<T>(
	string key,
	TimeSpan? ttl,
	Func<string?, T> getConverter,
	Func<T, string?> setConverter,
	bool isNullable = true
) : Meta(key, ttl, typeof(T), isNullable, val => val != null ? getConverter(val) : null)
{
	public Func<string?, T> ConvertGet => getConverter;
	public Func<T, string?> ConvertSet => setConverter;
}

public class Meta(string key, TimeSpan? ttl, Type type, bool isNullable, Func<string?, object?> cacheConverter)
{
	public Type                   Type         => type;
	public bool                   IsNullable   => isNullable;
	public string                 Key          => key;
	public TimeSpan               Ttl          => ttl ?? TimeSpan.FromDays(30);
	public Func<string?, object?> ConvertCache => cacheConverter;
}

public class StringMeta(string key, TimeSpan? ttl = null) : NonNullableMeta<string>(key, ttl, val => val, val => val);

public class NullableStringMeta(string key, TimeSpan? ttl = null) : Meta<string?>(key, ttl, val => val, val => val);

public class IntMeta(string key, TimeSpan? ttl = null)
	: NonNullableValueMeta<int>(key, ttl,
	                            int.Parse,
	                            val => val.ToString());

public class NullableIntMeta(string key, TimeSpan? ttl = null)
	: Meta<int?>(key, ttl, val => int.TryParse(val, out var result) ? result : null, val => val?.ToString());

public class NonNullableMeta<T>(string key, TimeSpan? ttl, Func<string, T> getConverter, Func<T, string> setConverter)
	: Meta<T>(key, ttl,
	          val => getConverter(val ?? throw new Exception($"Fetched meta value {key} was null")),
	          setConverter,
	          false) where T : class;

public class NonNullableValueMeta<T>(
	string key,
	TimeSpan? ttl,
	Func<string, T> getConverter,
	Func<T, string> setConverter
) : Meta<T>(key, ttl,
            val => getConverter(val ?? throw new Exception($"Fetched meta value {key} was null")),
            setConverter,
            false) where T : struct;