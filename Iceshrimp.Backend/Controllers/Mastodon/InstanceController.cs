using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Services;
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
public class InstanceController(DatabaseContext db, MetaService meta) : ControllerBase
{
	[HttpGet("/api/v1/instance")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InstanceInfoV1Response))]
	public async Task<IActionResult> GetInstanceInfoV1([FromServices] IOptionsSnapshot<Config> config)
	{
		var userCount =
			await db.Users.LongCountAsync(p => p.Host == null && !Constants.SystemUsers.Contains(p.UsernameLower));
		var noteCount     = await db.Notes.LongCountAsync(p => p.UserHost == null);
		var instanceCount = await db.Instances.LongCountAsync();

		var (instanceName, instanceDescription, adminContact) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		var res = new InstanceInfoV1Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Stats = new InstanceStats(userCount, noteCount, instanceCount)
		};

		return Ok(res);
	}

	[HttpGet("/api/v2/instance")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InstanceInfoV2Response))]
	public async Task<IActionResult> GetInstanceInfoV2([FromServices] IOptionsSnapshot<Config> config)
	{
		var cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
		var activeMonth = await db.Users.LongCountAsync(p => p.Host == null &&
		                                                     !Constants.SystemUsers.Contains(p.UsernameLower) &&
		                                                     p.LastActiveDate > cutoff);

		var (instanceName, instanceDescription, adminContact) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		var res = new InstanceInfoV2Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Usage = new InstanceUsage { Users = new InstanceUsersUsage { ActiveMonth = activeMonth } }
		};

		return Ok(res);
	}

	[HttpGet("/api/v1/custom_emojis")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<EmojiEntity>))]
	public async Task<IActionResult> GetCustomEmojis()
	{
		var res = await db.Emojis.Where(p => p.Host == null)
		                  .Select(p => new EmojiEntity
		                  {
			                  Id              = p.Id,
			                  Shortcode       = p.Name,
			                  Url             = p.PublicUrl,
			                  StaticUrl       = p.PublicUrl, //TODO
			                  VisibleInPicker = true
		                  })
		                  .ToListAsync();

		return Ok(res);
	}

	[HttpGet("/api/v1/instance/translation_languages")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, IEnumerable<string>>))]
	public IActionResult GetTranslationLanguages()
	{
		return Ok(new Dictionary<string, IEnumerable<string>>());
	}

	[HttpGet("/api/v1/instance/extended_description")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InstanceExtendedDescription))]
	public async Task<IActionResult> GetExtendedDescription()
	{
		var description = await meta.Get(MetaEntity.InstanceDescription);
		var res         = new InstanceExtendedDescription(description);
		return Ok(res);
	}
}