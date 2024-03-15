using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Services;

public class MetaService(IServiceScopeFactory scopeFactory, IDistributedCache cache)
{
	public async Task<string?> GetVapidPrivateKey() =>
		await cache.FetchAsync("cache:meta:vapidPrivateKey", TimeSpan.FromDays(30),
		                       async () => await Fetch("vapid_private_key"));

	public async Task<string?> GetVapidPublicKey() =>
		await cache.FetchAsync("cache:meta:vapidPublicKey", TimeSpan.FromDays(30),
		                       async () => await Fetch("vapid_public_key"));

	private async Task<string?> Fetch(string key)
	{
		var db = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
		return await db.MetaStore.Where(p => p.Key == key).Select(p => p.Value).FirstOrDefaultAsync();
	}

	//TODO
	// private interface IMeta
	// {
	// 	string Key { get; }
	// }
}