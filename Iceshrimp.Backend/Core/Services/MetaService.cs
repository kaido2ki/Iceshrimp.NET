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
		await cache.FetchAsync($"cache:meta:{meta.Key}", meta.Ttl, async () => await Fetch(meta));

	public async Task<T> GetValue<T>(Meta<T> meta) where T : struct =>
		await cache.FetchAsyncValue($"cache:meta:{meta.Key}", meta.Ttl, async () => await Fetch(meta));

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
		var entities = typeof(MetaEntity).GetMembers(BindingFlags.Static | BindingFlags.Public)
		                                 .OfType<FieldInfo>();

		foreach (var entity in entities)
		{
			var value = entity.GetValue(this);
			var type  = entity.FieldType;

			while (type?.GenericTypeArguments == null ||
			       type.GenericTypeArguments.Length == 0 ||
			       type.GetGenericTypeDefinition() != typeof(Meta<>))
			{
				if (type == typeof(object) || type == null)
					continue;

				type = type.BaseType;
			}

			var genericType = type.GenericTypeArguments.First();
			var task = typeof(MetaService)
			           .GetMethod(nameof(Get))!
			           .MakeGenericMethod(genericType)
			           .Invoke(this, [value]);

			await (Task)task!;
		}
	}

	private async Task<T> Fetch<T>(Meta<T> meta) => meta.GetConverter(await Fetch(meta.Key));

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

		await cache.SetAsync($"cache:meta:{key}", value, ttl, true);
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
}

public class Meta<T>(string key, TimeSpan? ttl, Func<string?, T> getConverter, Func<T, string?> setConverter)
{
	public string           Key          => key;
	public TimeSpan         Ttl          => ttl ?? TimeSpan.FromDays(30);
	public Func<string?, T> GetConverter => getConverter;
	public Func<T, string?> ConvertSet   => setConverter;
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
	          setConverter) where T : class;

public class NonNullableValueMeta<T>(
	string key,
	TimeSpan? ttl,
	Func<string, T> getConverter,
	Func<T, string> setConverter
) : Meta<T>(key, ttl,
            val => getConverter(val ?? throw new Exception($"Fetched meta value {key} was null")),
            setConverter) where T : struct;