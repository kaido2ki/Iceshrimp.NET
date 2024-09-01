using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class EmojiController(DatabaseContext db) : ControllerBase
{
	[HttpGet("/api/v1/pleroma/emoji")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<Dictionary<string, PleromaEmojiEntity>> GetCustomEmojis()
	{
		var emoji = await db.Emojis.Where(p => p.Host == null)
			.Select(p => KeyValuePair.Create(p.Name, new PleromaEmojiEntity
			{
				ImageUrl = new Uri(p.PublicUrl).AbsolutePath, // TODO: akkoma-fe expects *relative paths* instead of absolute URLs... i genuinely think this is unfixable without forking the fe
				Tags     = p.Category == null ? new string[0] : new string[] { p.Category }
			}))
			.ToArrayAsync();

		return new Dictionary<string, PleromaEmojiEntity>(emoji);
	}
}
