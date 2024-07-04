using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[LinkPagination(20, 80)]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/timelines")]
[Produces(MediaTypeNames.Application.Json)]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer, CacheService cache) : ControllerBase
{
	[HttpGet("home")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetHomeTimeline(PaginationQuery pq)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristic(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, filterHiddenListMembers: true)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return Ok(await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user, Filter.FilterContext.Home));
	}
}