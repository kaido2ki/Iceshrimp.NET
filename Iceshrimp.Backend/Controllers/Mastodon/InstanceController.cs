using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class InstanceController(DatabaseContext db) : Controller
{
	[HttpGet("/api/v1/instance")]
	[Produces("application/json")]
	public async Task<IActionResult> GetInstanceInfo([FromServices] IOptionsSnapshot<Config> config)
	{
		var userCount     = await db.Users.LongCountAsync(p => p.Host == null);
		var noteCount     = await db.Notes.LongCountAsync(p => p.UserHost == null);
		var instanceCount = await db.Instances.LongCountAsync();
		//TODO: admin contact

		var res = new InstanceInfoResponse(config.Value)
		{
			Stats = new InstanceStats(userCount, noteCount, instanceCount)
		};

		return Ok(res);
	}

	//TODO: v2
}