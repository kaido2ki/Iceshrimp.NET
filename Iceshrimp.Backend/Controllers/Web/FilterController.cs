using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for managing filters. Filters are used to hide or remove notes from their respective contexts.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/filters")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class FilterController(DatabaseContext db, EventService eventSvc) : ControllerBase
{
	/// <summary>
	/// List filters
	/// </summary>
	/// <remarks>Returns a list of filters.</remarks>
	/// <response code="200">List of filters</response>
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<FilterResponse>> GetFilters()
	{
		var user    = HttpContext.GetUserOrFail();
		var filters = await db.Filters.Where(p => p.User == user).ToListAsync();
		return FilterRenderer.RenderMany(filters);
	}

	/// <summary>
	/// Create filter
	/// </summary>
	/// <param name="request">Create filter request</param>
	/// <response code="200">New filter</response>
	[HttpPost]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<FilterResponse> CreateFilter(FilterRequest request)
	{
		var user = HttpContext.GetUserOrFail();

		var filter = new Filter
		{
			User     = user,
			Name     = request.Name,
			Expiry   = request.Expiry,
			Keywords = request.Keywords,
			Action   = (Filter.FilterAction)request.Action,
			Contexts = request.Contexts.Cast<Filter.FilterContext>().ToList()
		};

		db.Add(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterAdded(filter);
		return FilterRenderer.RenderOne(filter);
	}

	/// <summary>
	/// Update filter
	/// </summary>
	/// <param name="id">The filter's ID</param>
	/// <param name="request">Update filter request</param>
	/// <response code="200">Updated filter</response>
	[HttpPut("{id:long}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UpdateFilter(long id, FilterRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.FirstOrDefaultAsync(p => p.User == user && p.Id == id) ??
		             throw GracefulException.NotFound("Filter not found");

		filter.Name     = request.Name;
		filter.Expiry   = request.Expiry;
		filter.Keywords = request.Keywords;
		filter.Action   = (Filter.FilterAction)request.Action;
		filter.Contexts = request.Contexts.Cast<Filter.FilterContext>().ToList();

		await db.SaveChangesAsync();
		eventSvc.RaiseFilterUpdated(filter);
	}

	/// <summary>
	/// Remove filter
	/// </summary>
	/// <param name="id">The filter's ID</param>
	[HttpDelete("{id:long}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteFilter(long id)
	{
		var user = HttpContext.GetUserOrFail();
		var filter = await db.Filters.FirstOrDefaultAsync(p => p.User == user && p.Id == id) ??
		             throw GracefulException.NotFound("Filter not found");

		db.Remove(filter);
		await db.SaveChangesAsync();
		eventSvc.RaiseFilterRemoved(filter);
	}
}