using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Tags("Federation")]
[Route("/.well-known")]
[EnableCors("well-known")]
public class WellKnownController(IOptions<Config.InstanceSection> config, DatabaseContext db) : Controller
{
	[HttpGet("webfinger")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebFingerResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> WebFinger([FromQuery] string resource)
	{
		User? user;
		if (resource.StartsWith("acct:"))
		{
			var split = resource[5..].TrimStart('@').Split('@');
			if (split.Length > 2) return NotFound();
			if (split.Length == 2)
			{
				List<string> domains = [config.Value.AccountDomain, config.Value.WebDomain];
				if (!domains.Contains(split[1])) return NotFound();
			}

			user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == split[0].ToLowerInvariant() &&
			                                               p.Host == null);
		}
		else if (resource.StartsWith($"https://{config.Value.WebDomain}/users/"))
		{
			var id = resource[$"https://{config.Value.WebDomain}/users/".Length..];
			user = await db.Users.FirstOrDefaultAsync(p => p.Id == id && p.Host == null);
		}
		else
		{
			user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == resource.ToLowerInvariant() &&
			                                               p.Host == null);
		}

		if (user == null) return NotFound();

		var response = new WebFingerResponse
		{
			Subject = $"acct:{user.Username}@{config.Value.AccountDomain}",
			Links =
			[
				new WebFingerLink
				{
					Rel = "self", Type = "application/activity+json", Href = user.GetPublicUri(config.Value)
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
			]
		};

		return Ok(response);
	}

	[HttpGet("nodeinfo")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NodeInfoIndexResponse))]
	public IActionResult NodeInfo()
	{
		var response = new NodeInfoIndexResponse
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

		return Ok(response);
	}

	[HttpGet("host-meta")]
	[Produces("application/xrd+xml")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	public IActionResult HostMeta()
	{
		//TODO: use a proper xml serializer for this
		return
			Content($$"""<?xml version="1.0" encoding="UTF-8"?><XRD xmlns="http://docs.oasis-open.org/ns/xri/xrd-1.0"><Link rel="lrdd" type="application/xrd+xml" template="https://{{config.Value.WebDomain}}/.well-known/webfinger?resource={uri}"/></XRD>""",
			        "application/xrd+xml");
	}
}