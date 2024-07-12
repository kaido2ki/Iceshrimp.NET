using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/timelines")]
[Authenticate]
[LinkPagination(20, 40)]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class TimelineController(DatabaseContext db, NoteRenderer noteRenderer, CacheService cache) : ControllerBase
{
	[Authorize("read:statuses")]
	[HttpGet("home")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<StatusEntity>> GetHomeTimeline(MastodonPaginationQuery query)
	{
		var user      = HttpContext.GetUserOrFail();
		var heuristic = await QueryableTimelineExtensions.GetHeuristic(user, db, cache);
		return await db.Notes
		               .IncludeCommonProperties()
		               .FilterByFollowingAndOwn(user, db, heuristic)
		               .EnsureVisibleFor(user)
		               .FilterHidden(user, db, filterHiddenListMembers: true)
		               .FilterMutedThreads(user, db)
		               .Paginate(query, ControllerContext)
		               .PrecomputeVisibilities(user)
		               .RenderAllForMastodonAsync(noteRenderer, user, Filter.FilterContext.Home);
	}

	[Authorize("read:statuses")]
	[HttpGet("public")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<StatusEntity>> GetPublicTimeline(
		MastodonPaginationQuery query, TimelineSchemas.PublicTimelineRequest request
	)
	{
		var user = HttpContext.GetUserOrFail();
		return await db.Notes
		               .IncludeCommonProperties()
		               .HasVisibility(Note.NoteVisibility.Public)
		               .FilterByPublicTimelineRequest(request)
		               .FilterHidden(user, db)
		               .FilterMutedThreads(user, db)
		               .Paginate(query, ControllerContext)
		               .PrecomputeVisibilities(user)
		               .RenderAllForMastodonAsync(noteRenderer, user, Filter.FilterContext.Public);
	}

	[Authorize("read:statuses")]
	[HttpGet("tag/{hashtag}")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<StatusEntity>> GetHashtagTimeline(
		string hashtag, MastodonPaginationQuery query, TimelineSchemas.HashtagTimelineRequest request
	)
	{
		var user = HttpContext.GetUserOrFail();
		return await db.Notes
		               .IncludeCommonProperties()
		               .Where(p => p.Tags.Contains(hashtag.ToLowerInvariant()))
		               .FilterByHashtagTimelineRequest(request)
		               .FilterHidden(user, db)
		               .FilterMutedThreads(user, db)
		               .Paginate(query, ControllerContext)
		               .PrecomputeVisibilities(user)
		               .RenderAllForMastodonAsync(noteRenderer, user, Filter.FilterContext.Public);
	}

	[Authorize("read:lists")]
	[HttpGet("list/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<StatusEntity>> GetListTimeline(string id, MastodonPaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		if (!await db.UserLists.AnyAsync(p => p.Id == id && p.User == user))
			throw GracefulException.RecordNotFound();

		return await db.Notes
		               .IncludeCommonProperties()
		               .Where(p => db.UserListMembers.Any(l => l.UserListId == id && l.UserId == p.UserId))
		               .EnsureVisibleFor(user)
		               .FilterHidden(user, db)
		               .FilterMutedThreads(user, db)
		               .Paginate(query, ControllerContext)
		               .PrecomputeVisibilities(user)
		               .RenderAllForMastodonAsync(noteRenderer, user, Filter.FilterContext.Lists);
	}
}