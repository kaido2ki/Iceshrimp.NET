using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Federation;

[FederationApiController]
[Route("/.well-known")]
[EnableCors("well-known")]
public class WellKnownController(IOptions<Config.InstanceSection> config, DatabaseContext db) : ControllerBase
{
	[HttpGet("webfinger")]
	[Produces("application/jrd+json", "application/xrd+xml")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<WebFingerResponse> WebFinger([FromQuery] string resource)
	{
		User? user;
		if (resource.StartsWith($"https://{config.Value.WebDomain}/users/"))
		{
			var id = resource[$"https://{config.Value.WebDomain}/users/".Length..];
			user = await db.Users.FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser);
		}
		else
		{
			if (resource.StartsWith("acct:"))
				resource = resource[5..];

			var split = resource.TrimStart('@').Split('@');
			if (split.Length > 2) throw GracefulException.NotFound("User not found");
			if (split.Length == 2)
			{
				List<string> domains = [config.Value.AccountDomain, config.Value.WebDomain];
				if (!domains.Contains(split[1])) throw GracefulException.NotFound("User not found");
			}

			user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == split[0].ToLowerInvariant() &&
			                                               p.IsLocalUser);
		}

		if (user == null) throw GracefulException.NotFound("User not found");

		return new WebFingerResponse
		{
			Subject = $"acct:{user.Username}@{config.Value.AccountDomain}",
			Links =
			[
				new WebFingerLink
				{
					Rel  = "self",
					Type = "application/activity+json",
					Href = user.GetPublicUri(config.Value)
				},
				new WebFingerLink
				{
					Rel  = "self",
					Type = "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"",
					Href = user.GetPublicUri(config.Value)
				},
				new WebFingerLink
				{
					Rel  = "http://webfinger.net/rel/profile-page",
					Type = "text/html",
					Href = user.GetPublicUri(config.Value)
				},
				new WebFingerLink
				{
					Rel      = "http://ostatus.org/schema/1.0/subscribe",
					Template = $"https://{config.Value.WebDomain}/authorize-follow?acct={{uri}}"
				}
			],
			Aliases = [user.GetPublicUrl(config.Value), user.GetPublicUri(config.Value)]
		};
	}

	[HttpGet("nodeinfo")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResults(HttpStatusCode.OK)]
	public NodeInfoIndexResponse NodeInfo()
	{
		return new NodeInfoIndexResponse
		{
			Links =
			[
				new WebFingerLink
				{
					Rel  = "http://nodeinfo.diaspora.software/ns/schema/2.1",
					Href = $"https://{config.Value.WebDomain}/nodeinfo/2.1"
				},
				new WebFingerLink
				{
					Rel  = "http://nodeinfo.diaspora.software/ns/schema/2.0",
					Href = $"https://{config.Value.WebDomain}/nodeinfo/2.0"
				}
			]
		};
	}

	[HttpGet("host-meta")]
	[Produces("application/xrd+xml", "application/jrd+json")]
	[ProducesResults(HttpStatusCode.OK)]
	public HostMetaResponse HostMeta()
	{
		if (Request.Headers.Accept is []) Request.Headers.Accept = "application/xrd+xml";
		return new HostMetaResponse(config.Value.WebDomain);
	}

	[HttpGet("host-meta.json")]
	[Produces("application/jrd+json")]
	[ProducesResults(HttpStatusCode.OK)]
	public HostMetaResponse HostMetaJson() => new(config.Value.WebDomain);
}