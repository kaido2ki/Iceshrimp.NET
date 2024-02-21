using System.Net.Mime;
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
[Produces(MediaTypeNames.Application.Json)]
public class InstanceController(DatabaseContext db) : ControllerBase
{
	[HttpGet("/api/v1/instance")]
	public async Task<IActionResult> GetInstanceInfoV1([FromServices] IOptionsSnapshot<Config> config)
	{
		var userCount =
			await db.Users.LongCountAsync(p => p.Host == null && !Constants.SystemUsers.Contains(p.UsernameLower));
		var noteCount     = await db.Notes.LongCountAsync(p => p.UserHost == null);
		var instanceCount = await db.Instances.LongCountAsync();
		//TODO: admin contact

		var res = new InstanceInfoV1Response(config.Value)
		{
			Stats = new InstanceStats(userCount, noteCount, instanceCount)
		};

		return Ok(res);
	}

	[HttpGet("/api/v2/instance")]
	public async Task<IActionResult> GetInstanceInfoV2([FromServices] IOptionsSnapshot<Config> config)
	{
		var cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
		var activeMonth = await db.Users.LongCountAsync(p => p.Host == null &&
		                                                     !Constants.SystemUsers.Contains(p.UsernameLower) &&
		                                                     p.LastActiveDate > cutoff);
		//TODO: admin contact

		var res = new InstanceInfoV2Response(config.Value)
		{
			Usage = new InstanceUsage { Users = new InstanceUsersUsage { ActiveMonth = activeMonth } }
		};

		return Ok(res);
	}
}