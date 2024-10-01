using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Controllers.Web.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[LinkPagination(20, 80)]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/timelines")]
[Produces(MediaTypeNames.Application.Json)]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer, CacheService cache) : ControllerBase
{
	[HttpGet("home")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<PaginationWrapper<NoteResponse>> GetHomeTimeline(PaginationQueryCursor pq)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristic(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, filterHiddenListMembers: true)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		// TODO: we're parsing the cursor twice. once here and once in .Paginate()
		var cursor = pq.Cursor?.ParseCursor<NotePaginationCursor>();

		// TODO: get from attribute
		var limit = pq.Limit ?? 20;

		// @formatter:off
		var items = (await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user, Filter.FilterContext.Home)).ToList();
		if (cursor?.Up == true) items.Reverse();

		return new PaginationWrapper<NoteResponse>
		{
			PageUp   = items.Count > 0 ? new NotePaginationCursor { Id = items[0].Id, Up = true }.Serialize() : null,
			PageDown = (cursor?.Up == true && items.Count > 0) || items.Count >= limit ? new NotePaginationCursor { Id = items[^1].Id }.Serialize() : null,
			Items    = items,
		};
		// @formatter:on
	}
}
