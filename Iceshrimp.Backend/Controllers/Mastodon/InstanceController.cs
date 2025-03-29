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
public class InstanceController(
	IOptions<Config.InstanceSection> instance,
	DatabaseContext db,
	MetaService meta
) : ControllerBase
{
	[HttpGet("/api/v1/instance")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceInfoV1Response> GetInstanceInfoV1([FromServices] IOptionsSnapshot<Config> config)
	{
		var userCount =
			await db.Users.LongCountAsync(p => p.IsLocalUser && !Constants.SystemUsers.Contains(p.UsernameLower));
		var noteCount     = await db.Notes.LongCountAsync(p => p.UserHost == null);
		var instanceCount = await db.Instances.LongCountAsync();

		var (instanceName, instanceDescription, adminContact, bannerId) =
			await meta.GetManyAsync(MetaEntity.InstanceName, MetaEntity.InstanceDescription,
			                        MetaEntity.AdminContactEmail, MetaEntity.BannerFileId);

		// can't merge with above call since they're all nullable and this is not.
		var vapidKey = await meta.GetAsync(MetaEntity.VapidPublicKey);

		var banner = await db.DriveFiles.Where(p => p.Id == bannerId)
		                     .Select(p => p.PublicUrl ?? p.RawAccessUrl)
		                     .FirstOrDefaultAsync();

		return new InstanceInfoV1Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Stats   = new InstanceStats(userCount, noteCount, instanceCount),
			Pleroma = new PleromaInstanceExtensions { VapidPublicKey = vapidKey, Metadata = new InstanceMetadata() },
			Rules   = await GetRules(),
			ThumbnailUrl = banner
		};
	}

	[HttpGet("/api/v2/instance")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceInfoV2Response> GetInstanceInfoV2([FromServices] IOptionsSnapshot<Config> config)
	{
		var cutoff = DateTime.UtcNow - TimeSpan.FromDays(30);
		var activeMonth =
			await db.Users.LongCountAsync(p => p.IsLocalUser
			                                   && !Constants.SystemUsers.Contains(p.UsernameLower)
			                                   && p.LastActiveDate > cutoff);

		var (instanceName, instanceDescription, adminContact, iconId, bannerId) =
			await meta.GetManyAsync(MetaEntity.InstanceName, MetaEntity.InstanceDescription,
			                        MetaEntity.AdminContactEmail, MetaEntity.IconFileId, MetaEntity.BannerFileId);

		var favicon = await db.DriveFiles.Where(p => p.Id == iconId)
		                      .Select(p => new InstanceIcon(p.PublicUrl ?? p.RawAccessUrl, p.Properties.Width ?? 128,
		                                                    p.Properties.Height ?? 128))
		                      .FirstOrDefaultAsync();
		List<InstanceIcon> icons = favicon != null
			? [favicon]
			:
			[
				new
					InstanceIcon($"https://{config.Value.Instance.WebDomain}/_content/Iceshrimp.Assets.Branding/192.png",
					             192, 192),
				new
					InstanceIcon($"https://{config.Value.Instance.WebDomain}/_content/Iceshrimp.Assets.Branding/512.png",
					             512, 512)
			];

		// Mastodon expects an instance thumbnail, the mail wordmark isn't ideal but it's the closest thing ww have for a fallback
		var banner = await db.DriveFiles.Where(p => p.Id == bannerId)
		                     .Select(p => new InstanceThumbnail(p.PublicUrl ?? p.RawAccessUrl, p.Blurhash))
		                     .FirstOrDefaultAsync()
		             ?? new
			             InstanceThumbnail($"https://{config.Value.Instance.WebDomain}/_content/Iceshrimp.Assets.Branding/mail-wordmark.png",
			                               null);

		return new InstanceInfoV2Response(config.Value, instanceName, instanceDescription, adminContact)
		{
			Usage = new InstanceUsage { Users = new InstanceUsersUsage { ActiveMonth = activeMonth } },
			Rules = await GetRules(),
			Icons = icons,
			Thumbnail = banner
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
			               Url             = p.GetAccessUrl(instance.Value),
			               StaticUrl       = p.GetAccessUrl(instance.Value), //TODO
			               VisibleInPicker = true,
			               Category        = p.Category
		               })
		               .ToListAsync();
	}

	[HttpGet("/api/v1/instance/rules")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<RuleEntity>> GetRules()
	{
		return await db.Rules
		               .OrderBy(p => p.Order)
		               .ThenBy(p => p.Id)
		               .Select(p => new RuleEntity { Id = p.Id, Text = p.Text, Hint = p.Description })
		               .ToListAsync();
	}

	[HttpGet("/api/v1/instance/translation_languages")]
	[ProducesResults(HttpStatusCode.OK)]
	public Dictionary<string, IEnumerable<string>> GetTranslationLanguages() => new();

	[HttpGet("/api/v1/instance/extended_description")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<InstanceExtendedDescription> GetExtendedDescription()
	{
		var description = await meta.GetAsync(MetaEntity.InstanceDescription);
		return new InstanceExtendedDescription(description);
	}
}
