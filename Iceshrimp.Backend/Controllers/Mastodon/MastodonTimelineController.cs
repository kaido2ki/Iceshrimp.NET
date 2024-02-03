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
	public async Task<IActionResult> GetHomeTimeline(PaginationQuery query) {
		var user = HttpContext.GetOauthUser() ?? throw new GracefulException("Failed to get user from HttpContext");
		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .FilterByFollowingAndOwn(user)
		                  .EnsureVisibleFor(user)
		                  .FilterHiddenListMembers(user)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, 20, 40)
		                  .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}

	[AuthorizeOauth("read:statuses")]
	[HttpGet("public")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetPublicTimeline(PaginationQuery query) {
		var user = HttpContext.GetOauthUser() ?? throw new GracefulException("Failed to get user from HttpContext");

		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .HasVisibility(Note.NoteVisibility.Public)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, 20, 40)
		                  .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}
}