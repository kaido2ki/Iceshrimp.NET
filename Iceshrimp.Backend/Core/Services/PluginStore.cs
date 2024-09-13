using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text.Json;
using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

/// <summary>
///     This is needed because static fields in generic classes aren't shared between instances with different generic type
///     arguments
/// </summary>
file static class PluginStoreHelpers
{
	public static readonly AsyncKeyedLocker<Guid> KeyedLocker = new(o =>
	{
		o.PoolSize        = 10;
		o.PoolInitialFill = 2;
	});
}

public class PluginStore<TPlugin, TData>(DatabaseContext db) where TPlugin : IPlugin, new() where TData : new()
{
	private readonly IPlugin _plugin = new TPlugin();

	/// <exception cref="SerializationException"></exception>
	public async Task<TData> GetData()
	{
		return (await GetOrCreateData()).data;
	}

	/// <exception cref="SerializationException"></exception>
	public async Task<TResult> GetData<TResult>(Expression<Func<TData, TResult>> predicate)
	{
		var (_, data) = await GetOrCreateData();
		return predicate.Compile().Invoke(data);
	}

	/// <exception cref="SerializationException"></exception>
	public async Task UpdateData(Action<TData> updateAction)
	{
		using (await PluginStoreHelpers.KeyedLocker.LockAsync(_plugin.Id))
		{
			var (entry, data) = await GetOrCreateData();
			updateAction(data);
			UpdateEntryIfModified(entry, data);
			await db.SaveChangesAsync();
		}
	}

	private static void UpdateEntryIfModified(PluginStoreEntry entry, TData data)
	{
		var serialized = JsonSerializer.Serialize(data, JsonSerialization.Options);
		if (entry.Data != serialized)
			entry.Data = serialized;
	}

	/// <exception cref="SerializationException"></exception>
	private async Task<(PluginStoreEntry entry, TData data)> GetOrCreateData()
	{
		TData data;
		var   entry = await db.PluginStore.FirstOrDefaultAsync(p => p.Id == _plugin.Id);
		if (entry == null)
		{
			data = new TData();
			entry = new PluginStoreEntry
			{
				Id   = _plugin.Id,
				Name = _plugin.Name,
				Data = JsonSerializer.Serialize(data)
			};
			db.Add(entry);
		}
		else
		{
			data = JsonSerializer.Deserialize<TData>(entry.Data, JsonSerialization.Options) ??
			       throw new SerializationException("Failed to deserialize plugin data");
		}

		return (entry, data);
	}
}