using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We need IOptionsSnapshot for config hot reload")]
public class FederationControlService(
	IOptionsSnapshot<Config.SecuritySection> options,
	IOptions<Config.InstanceSection> instance,
	DatabaseContext db
)
{
	//TODO: we need some level of caching here
	public async Task<bool> ShouldBlockAsync(params string?[] hosts)
	{
		if (hosts.All(p => p == instance.Value.WebDomain || p == instance.Value.AccountDomain)) return false;

		var finalHosts = hosts.Where(p => p != null)
		                      .Cast<string>()
		                      .Distinct()
		                      .Select(p => p.StartsWith("http://") || p.StartsWith("https://") ? new Uri(p).Host : p)
		                      .Select(p => p.ToPunycodeLower())
		                      .ToArray();

		// We want to check for fully qualified domains *and* subdomains of them
		if (options.Value.FederationMode == Enums.FederationMode.AllowList)
		{
			return !await db.AllowedInstances.AnyAsync(p => finalHosts.Any(host => host == p.Host ||
				                                                               host.EndsWith("." + p.Host)));
		}

		return await db.BlockedInstances.AnyAsync(p => finalHosts.Any(host => host == p.Host ||
		                                                                      host.EndsWith("." + p.Host)));
	}
}