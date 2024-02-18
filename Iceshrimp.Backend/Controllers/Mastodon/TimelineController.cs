using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/timelines")]
[Authenticate]
[LinkPagination(20, 40)]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer, IDistributedCache cache) : Controller
{
	[Authorize("read:statuses")]
	[HttpGet("home")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StatusEntity>))]
	public async Task<IActionResult> GetHomeTimeline(MastodonPaginationQuery query)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableExtensions.GetHeuristic(user, db, cache);

		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .FilterByFollowingAndOwn(user, db, heuristic)
		                  .EnsureVisibleFor(user)
		                  .FilterHiddenListMembers(user)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer, user);

		return Ok(res);
	}

	[Authorize("read:statuses")]
	[HttpGet("public")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<StatusEntity>))]
	public async Task<IActionResult> GetPublicTimeline(MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();

		var res = await db.Notes
		                  .IncludeCommonProperties()
		                  .HasVisibility(Note.NoteVisibility.Public)
		                  .FilterBlocked(user)
		                  .FilterMuted(user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeVisibilities(user)
		                  .RenderAllForMastodonAsync(noteRenderer, user);

		return Ok(res);
	}
}