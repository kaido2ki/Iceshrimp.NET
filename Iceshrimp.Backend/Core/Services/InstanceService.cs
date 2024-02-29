using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Core.Services;

public class InstanceService(DatabaseContext db, HttpClient httpClient)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	public async Task<Instance> GetUpdatedInstanceMetadataAsync(string host, string webDomain)
	{
		host = host.ToLowerInvariant();
		var instance = db.Instances.FirstOrDefault(p => p.Host == host);
		if (instance == null)
		{
			instance = new Instance
			{
				Id                 = IdHelpers.GenerateSlowflakeId(),
				Host               = host,
				CaughtAt           = DateTime.UtcNow,
				LastCommunicatedAt = DateTime.UtcNow,
			};
			await db.AddAsync(instance);
		}

		if (instance.NeedsUpdate && !KeyedLocker.IsInUse(host))
		{
			using (await KeyedLocker.LockAsync(host))
			{
				instance.InfoUpdatedAt = DateTime.UtcNow;
				var nodeinfo = await GetNodeInfoAsync(webDomain);
				if (nodeinfo != null)
				{
					instance.Name        = nodeinfo.Metadata?.NodeName;
					instance.Description = nodeinfo.Metadata?.NodeDescription;
					//instance.FaviconUrl = TODO,
					//instance.FollowersCount = TODO,
					//instance.FollowingCount = TODO,
					//instance.IconUrl = TODO,
					instance.MaintainerName    = nodeinfo.Metadata?.Maintainer?.Name;
					instance.MaintainerEmail   = nodeinfo.Metadata?.Maintainer?.Email;
					instance.OpenRegistrations = nodeinfo.OpenRegistrations;
					instance.SoftwareName      = nodeinfo.Software?.Name;
					instance.SoftwareVersion   = nodeinfo.Software?.Version;
					instance.ThemeColor        = nodeinfo.Metadata?.ThemeColor;
				}
			}
		}

		await db.SaveChangesAsync();
		return instance;
	}

	private async Task<NodeInfoResponse?> GetNodeInfoAsync(string webDomain)
	{
		try
		{
			var res =
				await httpClient.GetFromJsonAsync<NodeInfoIndexResponse>($"https://{webDomain}/.well-known/nodeinfo");

			var url = res?.Links.FirstOrDefault(p => p.Rel == "http://nodeinfo.diaspora.software/ns/schema/2.1") ??
			          res?.Links.FirstOrDefault(p => p.Rel == "http://nodeinfo.diaspora.software/ns/schema/2.0");

			if (url == null) return null;

			return await httpClient.GetFromJsonAsync<NodeInfoResponse>(url.Href);
		}
		catch
		{
			return null;
		}
	}

	public async Task UpdateInstanceStatusAsync(string host, string webDomain)
	{
		var instance = await GetUpdatedInstanceMetadataAsync(host, webDomain);

		instance.LastCommunicatedAt      = DateTime.UtcNow;
		instance.LatestRequestReceivedAt = DateTime.UtcNow;

		await db.SaveChangesAsync();
	}

	public async Task UpdateInstanceStatusAsync(string host, string webDomain, int statusCode, bool notResponding)
	{
		var instance = await GetUpdatedInstanceMetadataAsync(host, webDomain);

		instance.LatestStatus        = statusCode;
		instance.LatestRequestSentAt = DateTime.UtcNow;

		if (notResponding)
		{
			instance.IsNotResponding = true;
		}
		else
		{
			instance.IsNotResponding    = false;
			instance.LastCommunicatedAt = DateTime.UtcNow;
		}

		await db.SaveChangesAsync();
	}
}