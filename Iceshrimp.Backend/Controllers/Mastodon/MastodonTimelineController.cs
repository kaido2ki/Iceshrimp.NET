using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[ApiController]
[Tags("Mastodon")]
[Route("/api/v1/timelines")]
[AuthenticateOauth]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
public class MastodonTimelineController(DatabaseContext db, NoteRenderer noteRenderer) : Controller {
	[AuthorizeOauth("read:statuses")]
	[HttpGet("home")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetHomeTimeline() {
		var user = HttpContext.GetOauthUser() ?? throw new GracefulException("Failed to get user from HttpContext");
		var notes = await db.Notes
		                    .WithIncludes()
		                    .IsFollowedBy(user)
		                    .OrderByIdDesc()
		                    .Take(40)
		                    .ToListAsync();

		return Ok(notes.Select(async p => await noteRenderer.RenderAsync(p)));
	}

	[AuthorizeOauth("read:statuses")]
	[HttpGet("public")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetPublicTimeline() {
		var notes = await db.Notes
		                    .WithIncludes()
		                    .HasVisibility(Note.NoteVisibility.Public)
		                    .OrderByIdDesc()
		                    .Take(40)
		                    .ToListAsync();

		return Ok(notes.Select(async p => await noteRenderer.RenderAsync(p)));
	}
}