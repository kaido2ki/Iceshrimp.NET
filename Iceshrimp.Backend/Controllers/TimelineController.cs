using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/v1/timeline")]
[Produces(MediaTypeNames.Application.Json)]
public class TimelineController(DatabaseContext db, IDistributedCache cache) : Controller
{
	[HttpGet("home")]
	[Authenticate, Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetHomeTimeline(PaginationQuery pq)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableExtensions.GetHeuristic(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHiddenListMembers(user)
		                    .FilterBlocked(user)
		                    .FilterMuted(user)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return Ok(NoteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility()));
	}
}