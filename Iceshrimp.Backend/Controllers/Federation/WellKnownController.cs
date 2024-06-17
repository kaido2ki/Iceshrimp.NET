using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Xml.Serialization;
using Iceshrimp.Backend.Controllers.Federation.Attributes;
using Iceshrimp.Backend.Controllers.Federation.Schemas;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
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
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebFingerResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> WebFinger([FromQuery] string resource)
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
			if (split.Length > 2) return NotFound();
			if (split.Length == 2)
			{
				List<string> domains = [config.Value.AccountDomain, config.Value.WebDomain];
				if (!domains.Contains(split[1])) return NotFound();
			}

			user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == split[0].ToLowerInvariant() &&
			                                               p.IsLocalUser);
		}

		if (user == null) return NotFound();

		var response = new WebFingerResponse
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
	[Produces("application/xrd+xml", "application/jrd+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HostMetaJsonResponse))]
	public IActionResult HostMeta()
	{
		var accept = Request.Headers.Accept.OfType<string>()
		                    .SelectMany(p => p.Split(","))
		                    .Select(MediaTypeWithQualityHeaderValue.Parse)
		                    .Select(p => p.MediaType)
		                    .ToList();

		if (accept.Contains("application/jrd+json") || accept.Contains("application/json"))
			return HostMetaJson();

		var obj        = new HostMetaXmlResponse(config.Value.WebDomain);
		var serializer = new XmlSerializer(obj.GetType());
		var writer     = new Utf8StringWriter();

		serializer.Serialize(writer, obj);
		return Content(writer.ToString(), "application/xrd+xml");
	}

	[HttpGet("host-meta.json")]
	[Produces("application/jrd+json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HostMetaJsonResponse))]
	public IActionResult HostMetaJson()
	{
		return Ok(new HostMetaJsonResponse(config.Value.WebDomain));
	}

	private class Utf8StringWriter : StringWriter
	{
		public override Encoding Encoding => Encoding.UTF8;
	}
}