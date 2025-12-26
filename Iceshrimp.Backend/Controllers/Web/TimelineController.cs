using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
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
[EnableCors("iceshrimp")]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer, CacheService cache) : ControllerBase
{
	[HttpGet("home")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetHomeTimeline(PaginationQuery pq)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, filterHiddenListMembers: true)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Home);
	}

	[HttpGet("local")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetLocalTimeline(PaginationQuery pq)
	{
		//TODO: verify heuristic is accurate and/or even necessary for this query
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => p.UserHost == null)
		                    .FilterByPublicFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Public);
	}

	[HttpGet("social")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetSocialTimeline(PaginationQuery pq)
	{
		//TODO: verify heuristic is accurate and/or even necessary for this query
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByFollowingOwnAndLocal(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, filterHiddenListMembers: true)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Public);
	}

	[HttpGet("bubble")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetBubbleTimeline(PaginationQuery pq)
	{
		//TODO: verify heuristic is accurate and/or even necessary for this query
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => p.UserHost == null || db.BubbleInstances.Any(i => i.Host == p.UserHost))
		                    .FilterByPublicFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, filterHiddenListMembers: true)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Public);
	}

	[HttpGet("global")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetGlobalTimeline(PaginationQuery pq)
	{
		//TODO: verify heuristic is accurate and/or even necessary for this query
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .FilterByPublicFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Public);
	}

	[HttpGet("bookmarks")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetBookmarksTimeline(PaginationQuery pq)
	{
		var user  = HttpContext.GetUserOrFail();
		var notes = await db.NoteBookmarks
		                    .Where(p => p.User == user)
		                    .IncludeCommonProperties()
		                    .Select(p => p.Note)
		                    .EnsureVisibleFor(user)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user);
	}

	[HttpGet("list/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetListTimeline(string id, PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();

		if (!await db.UserLists.AnyAsync(p => p.Id == id && p.User == user))
			throw GracefulException.NotFound("List not found");

		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => db.UserListMembers.Any(l => l.UserListId == id && l.UserId == p.UserId))
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user);
	}

	[HttpGet("remote/{instance}")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetRemoteTimeline(string instance, PaginationQuery pq)
	{
		//TODO: verify heuristic is accurate and/or even necessary for this query
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristicAsync(user, db, cache);
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => p.UserHost == instance)
		                    .FilterByPublicFollowingAndOwn(user, db, heuristic)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .FilterMutedThreads(user, db)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user,
		                                          Filter.FilterContext.Public);
	}
}
