using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
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
public class AnnouncementController(DatabaseContext db, AnnouncementRenderer renderer) : ControllerBase
{
	[HttpGet]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	public async Task<IEnumerable<AnnouncementEntity>> GetAnnouncements(
		[FromQuery(Name = "with_dismissed")] bool withDismissed
	)
	{
		var user = HttpContext.GetUserOrFail();

		var announcements = await db.Announcements
		                            .Where(p => withDismissed || !p.IsReadBy(user))
		                            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
		                            .ToListAsync();

		return await renderer.RenderManyAsync(announcements, user);
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