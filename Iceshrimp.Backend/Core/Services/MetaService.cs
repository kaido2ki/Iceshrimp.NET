using EntityFramework.Exceptions.Common;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class MetaService(IServiceScopeFactory scopeFactory)
{
	public async Task<T> Get<T>(Meta<T> meta) => meta.ConvertGet(await Fetch(meta.Key));

	public async Task<IList<T>> GetMany<T>(params Meta<T>[] entities)
	{
		var res = await FetchMany(entities.Select(p => p.Key));
		return entities.Select(p => p.ConvertGet(res.GetValueOrDefault(p.Key, null))).ToArray();
	}

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

	public async Task Set<T>(Meta<T> meta, T value) => await Set(meta.Key, meta.ConvertSet(value));

	// Ensures the table is in memory (we could use pg_prewarm for this but that extension requires superuser privileges to install)
	public async Task WarmupCache() => await GetDbContext().MetaStore.ToListAsync();

	private async Task<string?> Fetch(string key) =>
		await GetDbContext().MetaStore.Where(p => p.Key == key).Select(p => p.Value).FirstOrDefaultAsync();

	private async Task<Dictionary<string, string?>> FetchMany(IEnumerable<string> keys) =>
		await GetDbContext().MetaStore.Where(p => keys.Contains(p.Key)).ToDictionaryAsync(p => p.Key, p => p.Value);

	private async Task Set(string key, string? value)
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
	Func<string?, T> getConverter,
	Func<T, string?> setConverter,
	bool isNullable = true
) : Meta(key, typeof(T), isNullable, val => val != null ? getConverter(val) : null)
{
	public Func<string?, T> ConvertGet => getConverter;
	public Func<T, string?> ConvertSet => setConverter;
}

public class Meta(string key, Type type, bool isNullable, Func<string?, object?> cacheConverter)
{
	public Type                   Type         => type;
	public bool                   IsNullable   => isNullable;
	public string                 Key          => key;
	public Func<string?, object?> ConvertCache => cacheConverter;
}

public class StringMeta(string key) : NonNullableMeta<string>(key, val => val, val => val);

public class NullableStringMeta(string key) : Meta<string?>(key, val => val, val => val);

public class IntMeta(string key) : NonNullableMeta<int>(key, int.Parse, val => val.ToString());

public class NullableIntMeta(string key)
	: Meta<int?>(key, val => int.TryParse(val, out var result) ? result : null, val => val?.ToString());

public class NonNullableMeta<T>(string key, Func<string, T> getConverter, Func<T, string> setConverter)
	: Meta<T>(key, val => getConverter(val ?? throw new Exception($"Fetched meta value {key} was null")),
	          setConverter,
	          false);