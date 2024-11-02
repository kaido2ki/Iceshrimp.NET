using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/announcements")]
[EnableCors("mastodon")]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class AnnouncementController(DatabaseContext db, MfmConverter mfmConverter) : ControllerBase
{
	[HttpGet]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IEnumerable<AnnouncementEntity>> GetAnnouncements(
		[FromQuery(Name = "with_dismissed")] bool withDismissed
	)
	{
		var user          = HttpContext.GetUserOrFail();
		var announcements = db.Announcements.AsQueryable();

		if (!withDismissed)
			announcements = announcements.Where(p => p.IsReadBy(user));

		var res = await announcements.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
		                             .Select(p => new AnnouncementEntity
		                             {
			                             Id          = p.Id,
			                             PublishedAt = p.CreatedAt.ToStringIso8601Like(),
			                             UpdatedAt   = (p.UpdatedAt ?? p.CreatedAt).ToStringIso8601Like(),
			                             IsRead      = p.IsReadBy(user),
			                             Content = $"""
			                                        **{p.Title}**
			                                        {p.Text}
			                                        """,
			                             Mentions = new List<MentionEntity>(), //TODO
			                             Emoji    = new List<EmojiEntity>()    //TODO
		                             })
		                             .ToListAsync();

		await res.Select(async p => p.Content = await mfmConverter.ToHtmlAsync(p.Content, [], null)).AwaitAllAsync();
		return res;
	}

	[HttpPost("{id}/dismiss")]
	[Authorize("write:accounts")]
	[OverrideResultType<object>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<object> DismissAnnouncement(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id) ??
		                   throw GracefulException.NotFound("Announcement not found");

		if (await db.Announcements.AnyAsync(p => p == announcement && !p.IsReadBy(user)))
		{
			var announcementRead = new AnnouncementRead
			{
				Id           = IdHelpers.GenerateSnowflakeId(),
				CreatedAt    = DateTime.UtcNow,
				Announcement = announcement,
				User         = user
			};
			await db.AnnouncementReads.AddAsync(announcementRead);
			await db.SaveChangesAsync();
		}

		return new object();
	}

	[HttpPut("{id}/reactions/{name}")]
	[Authorize("write:favourites")]
	[ProducesErrors(HttpStatusCode.NotImplemented)]
	public IActionResult ReactToAnnouncement(string id, string name) =>
		throw new GracefulException(HttpStatusCode.NotImplemented,
		                            "Iceshrimp.NET does not support this endpoint due to database schema differences to Mastodon");

	[HttpDelete("{id}/reactions/{name}")]
	[Authorize("write:favourites")]
	[ProducesErrors(HttpStatusCode.NotImplemented)]
	public IActionResult RemoveAnnouncementReaction(string id, string name) =>
		throw new GracefulException(HttpStatusCode.NotImplemented,
		                            "Iceshrimp.NET does not support this endpoint due to database schema differences to Mastodon");
}