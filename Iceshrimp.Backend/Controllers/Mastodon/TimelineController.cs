using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
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

[MastodonApiController]
[Route("/api/v1/timelines")]
[Authenticate]
[LinkPagination(20, 40)]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer) : Controller {
	[Authorize("read:statuses")]
	[HttpGet("home")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetHomeTimeline(PaginationQuery query) {
		var user = HttpContext.GetUserOrFail();
		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .FilterByFollowingAndOwn(user)
		                  .EnsureVisibleFor(user)
		                  .FilterHiddenListMembers(user)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}

	[Authorize("read:statuses")]
	[HttpGet("public")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Status>))]
	public async Task<IActionResult> GetPublicTimeline(PaginationQuery query) {
		var user = HttpContext.GetUserOrFail();

		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .HasVisibility(Note.NoteVisibility.Public)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer);

		return Ok(res);
	}
}