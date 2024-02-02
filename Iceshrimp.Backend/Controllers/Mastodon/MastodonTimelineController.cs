using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
		var res = await db.Notes
		                  .WithIncludes()
		                  .FilterByFollowingAndOwn(user)
		                  .OrderByIdDesc()
		                  .Take(40)
		                  .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}

	[AuthorizeOauth("read:statuses")]
	[HttpGet("public")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetPublicTimeline() {
		var res = await db.Notes
		                    .WithIncludes()
		                    .HasVisibility(Note.NoteVisibility.Public)
		                    .OrderByIdDesc()
		                    .Take(40)
		                    .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}
}