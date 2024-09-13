using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
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
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceInfoV1Response> GetInstanceInfoV1([FromServices] IOptionsSnapshot<Config> config)
	{
		var userCount =
			await db.Users.LongCountAsync(p => p.IsLocalUser && !Constants.SystemUsers.Contains(p.UsernameLower));
		var noteCount     = await db.Notes.LongCountAsync(p => p.UserHost == null);
		var instanceCount = await db.Instances.LongCountAsync();

		var (instanceName, instanceDescription, adminContact) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		// can't merge with above call since they're all nullable and this is not.
		var vapidKey = await meta.Get(MetaEntity.VapidPublicKey);

		return new InstanceInfoV1Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Stats   = new InstanceStats(userCount, noteCount, instanceCount),
			Pleroma = new PleromaInstanceExtensions { VapidPublicKey = vapidKey, Metadata = new InstanceMetadata() }
		};
	}

	[HttpGet("/api/v2/instance")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceInfoV2Response> GetInstanceInfoV2([FromServices] IOptionsSnapshot<Config> config)
	{
		var cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
		var activeMonth = await db.Users.LongCountAsync(p => p.IsLocalUser &&
		                                                     !Constants.SystemUsers.Contains(p.UsernameLower) &&
		                                                     p.LastActiveDate > cutoff);

		var (instanceName, instanceDescription, adminContact) =
			await meta.GetMany(MetaEntity.InstanceName, MetaEntity.InstanceDescription, MetaEntity.AdminContactEmail);

		return new InstanceInfoV2Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Usage = new InstanceUsage { Users = new InstanceUsersUsage { ActiveMonth = activeMonth } }
		};
	}

	[HttpGet("/api/v1/custom_emojis")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<EmojiEntity>> GetCustomEmojis()
	{
		return await db.Emojis.Where(p => p.Host == null)
		               .Select(p => new EmojiEntity
		               {
			               Id              = p.Id,
			               Shortcode       = p.Name,
			               Url             = p.PublicUrl,
			               StaticUrl       = p.PublicUrl, //TODO
			               VisibleInPicker = true,
			               Category        = p.Category
		               })
		               .ToListAsync();
	}

	[HttpGet("/api/v1/instance/translation_languages")]
	[ProducesResults(HttpStatusCode.OK)]
	public Dictionary<string, IEnumerable<string>> GetTranslationLanguages() => new();

	[HttpGet("/api/v1/instance/extended_description")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceExtendedDescription> GetExtendedDescription()
	{
		var description = await meta.Get(MetaEntity.InstanceDescription);
		return new InstanceExtendedDescription(description);
	}
}