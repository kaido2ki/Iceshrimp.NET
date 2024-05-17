using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Queues;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v2/filters")]
[Authenticate]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class FilterController(DatabaseContext db, QueueService queueSvc, EventService eventSvc) : ControllerBase
{
	[HttpGet]
	[Authorize("read:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FilterEntity>))]
	public async Task<IActionResult> GetFilters()
	{
		var user    = HttpContext.GetUserOrFail();
		var filters = await db.Filters.Where(p => p.User == user).ToListAsync();
		var res     = filters.Select(FilterRenderer.RenderOne);

		return Ok(res);
	}

	[HttpGet("{id:long}")]
	[Authorize("read:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FilterEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetFilter(long id)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == id).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		return Ok(FilterRenderer.RenderOne(filter));
	}

	[HttpPost]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FilterEntity>))]
	public async Task<IActionResult> CreateFilter([FromHybrid] FilterSchemas.CreateFilterRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var action = request.Action switch
		{
			"warn" => Filter.FilterAction.Warn,
			"hide" => Filter.FilterAction.Hide,
			_      => throw GracefulException.BadRequest($"Unknown action: {request.Action}")
		};

		var context = request.Context.Select(p => p switch
		{
			"home"          => Filter.FilterContext.Home,
			"notifications" => Filter.FilterContext.Notifications,
			"public"        => Filter.FilterContext.Public,
			"thread"        => Filter.FilterContext.Threads,
			"account"       => Filter.FilterContext.Accounts,
			_               => throw GracefulException.BadRequest($"Unknown filter context: {p}")
		});

		var contextList = context.ToList();
		if (contextList.Contains(Filter.FilterContext.Home))
			contextList.Add(Filter.FilterContext.Lists);

		DateTime? expiry = request.ExpiresIn.HasValue
			? DateTime.UtcNow + TimeSpan.FromSeconds(request.ExpiresIn.Value)
			: null;

		var keywords = request.Keywords.Select(p => p.WholeWord ? $"\"{p.Keyword}\"" : p.Keyword).ToList();

		var filter = new Filter
		{
			Name     = request.Title,
			User     = user,
			Contexts = contextList,
			Action   = action,
			Expiry   = expiry,
			Keywords = keywords
		};

		db.Add(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterAdded(this, filter);

		if (expiry.HasValue)
		{
			var data = new FilterExpiryJobData { FilterId = filter.Id };
			await queueSvc.BackgroundTaskQueue.ScheduleAsync(data, expiry.Value);
		}

		return Ok(FilterRenderer.RenderOne(filter));
	}

	[HttpPut("{id:long}")]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FilterEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UpdateFilter(long id, [FromHybrid] FilterSchemas.UpdateFilterRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.FirstOrDefaultAsync(p => p.User == user && p.Id == id) ??
		             throw GracefulException.RecordNotFound();

		var action = request.Action switch
		{
			"warn" => Filter.FilterAction.Warn,
			"hide" => Filter.FilterAction.Hide,
			_      => throw GracefulException.BadRequest($"Unknown action: {request.Action}")
		};

		var context = request.Context.Select(p => p switch
		{
			"home"          => Filter.FilterContext.Home,
			"notifications" => Filter.FilterContext.Notifications,
			"public"        => Filter.FilterContext.Public,
			"thread"        => Filter.FilterContext.Threads,
			"account"       => Filter.FilterContext.Accounts,
			_               => throw GracefulException.BadRequest($"Unknown filter context: {p}")
		});

		var contextList = context.ToList();
		if (contextList.Contains(Filter.FilterContext.Home))
			contextList.Add(Filter.FilterContext.Lists);

		DateTime? expiry = request.ExpiresIn.HasValue
			? DateTime.UtcNow + TimeSpan.FromSeconds(request.ExpiresIn.Value)
			: null;

		foreach (var kw in request.Keywords.Where(p => p is { Id: not null, Destroy: false }))
			filter.Keywords[int.Parse(kw.Id!.Split('-')[1])] = kw.WholeWord ? $"\"{kw.Keyword}\"" : kw.Keyword;

		var destroy = request.Keywords.Where(p => p is { Id: not null, Destroy: true }).Select(p => p.Id);
		var @new = request.Keywords.Where(p => p.Id == null)
		                  .Select(p => p.WholeWord ? $"\"{p.Keyword}\"" : p.Keyword)
		                  .ToList();

		var keywords = filter.Keywords.Where((_, i) => !destroy.Contains(i.ToString())).Concat(@new).ToList();

		filter.Name     = request.Title;
		filter.Contexts = contextList;
		filter.Action   = action;
		filter.Expiry   = expiry;
		filter.Keywords = keywords;

		db.Update(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterUpdated(this, filter);

		if (expiry.HasValue)
		{
			var data = new FilterExpiryJobData { FilterId = filter.Id };
			await queueSvc.BackgroundTaskQueue.ScheduleAsync(data, expiry.Value);
		}

		return Ok(FilterRenderer.RenderOne(filter));
	}

	[HttpDelete("{id:long}")]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> DeleteFilter(long id)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == id).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		db.Remove(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterRemoved(this, filter);

		return Ok(new object());
	}

	[HttpGet("{id:long}/keywords")]
	[Authorize("read:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FilterKeyword>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetFilterKeywords(long id)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == id).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		return Ok(filter.Keywords.Select((p, i) => new FilterKeyword(p, filter.Id, i)));
	}

	[HttpPost("{id:long}/keywords")]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FilterKeyword))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> AddFilterKeyword(
		long id, [FromHybrid] FilterSchemas.FilterKeywordsAttributes request
	)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == id).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		var keyword = request.WholeWord ? $"\"{request.Keyword}\"" : request.Keyword;
		filter.Keywords.Add(keyword);

		db.Update(keyword);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterUpdated(this, filter);

		return Ok(new FilterKeyword(keyword, filter.Id, filter.Keywords.Count - 1));
	}

	[HttpGet("keywords/{filterId:long}-{keywordId:int}")]
	[Authorize("read:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FilterKeyword))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetFilterKeyword(long filterId, int keywordId)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == filterId).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		if (filter.Keywords.Count < keywordId)
			throw GracefulException.RecordNotFound();

		return Ok(new FilterKeyword(filter.Keywords[keywordId], filter.Id, keywordId));
	}

	[HttpPut("keywords/{filterId:long}-{keywordId:int}")]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FilterKeyword))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UpdateFilterKeyword(
		long filterId, int keywordId, [FromHybrid] FilterSchemas.FilterKeywordsAttributes request
	)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == filterId).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		if (filter.Keywords.Count < keywordId)
			throw GracefulException.RecordNotFound();

		filter.Keywords[keywordId] = request.WholeWord ? $"\"{request.Keyword}\"" : request.Keyword;
		db.Update(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterUpdated(this, filter);

		return Ok(new FilterKeyword(filter.Keywords[keywordId], filter.Id, keywordId));
	}

	[HttpDelete("keywords/{filterId:long}-{keywordId:int}")]
	[Authorize("write:filters")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> DeleteFilterKeyword(long filterId, int keywordId)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.Where(p => p.User == user && p.Id == filterId).FirstOrDefaultAsync() ??
		             throw GracefulException.RecordNotFound();

		if (filter.Keywords.Count < keywordId)
			throw GracefulException.RecordNotFound();

		filter.Keywords.RemoveAt(keywordId);
		db.Update(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterUpdated(this, filter);

		return Ok(new object());
	}

	//TODO: status filters (first: what are they even for?)
}