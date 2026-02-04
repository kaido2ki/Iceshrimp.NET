using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations that aren't well categorized and may be moved to a more appropriate section at a later date.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[Tags("Miscellaneous")]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/misc")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class MiscController(DatabaseContext db, NoteRenderer noteRenderer, BiteService biteSvc) : ControllerBase
{
	/// <summary>
	/// Bite back
	/// </summary>
	/// <remarks>Bite a user back in response to a Bite activity</remarks>
	/// <param name="id">The Bite's ID</param>
	[HttpPost("bite_back/{id}")]
	[Authenticate]
	[Authorize]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage",
	                 Justification = "IncludeCommonProperties")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery",
	                 Justification = "IncludeCommonProperties")]
	public async Task BiteBack(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var target = await db.Bites
		                     .IncludeCommonProperties()
		                     .Where(p => p.Id == id)
		                     .FirstOrDefaultAsync() ??
		             throw GracefulException.NotFound("Bite not found");

		if (user.Id != (target.TargetUserId ?? target.TargetNote?.UserId ?? target.TargetBite?.UserId))
			throw GracefulException.BadRequest("You can only bite back at a user who bit you");

		await biteSvc.BiteAsync(user, target);
	}

	/// <summary>
	/// Get muted threads
	/// </summary>
	/// <remarks>Returns a paginated list of note threads that are muted by the user.</remarks>
	/// <param name="pq">Pagination query</param>
	/// <response code="200">Paginated list of muted note threads</response>
	[HttpGet("muted_threads")]
	[LinkPagination(20, 40)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetMutedThreads(PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => db.NoteThreadMutings.Any(m => m.ThreadId == p.ThreadId))
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, false, false)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderManyAsync(notes.EnforceRenoteReplyVisibility(), user);
	}

	/// <summary>
	/// Get user status
	/// </summary>
	/// <remarks>Returns status information for the user.</remarks>
	/// <response code="200">Status information</response>
	[HttpGet("status")]
	[EnableRateLimiting("sliding")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<StatusResponse> GetStatus()
	{
		var user = HttpContext.GetUserOrFail();

		var unreadAnnouncements = await db.Announcements.AnyAsync(p => p.ReadBy.All(i => i != user));

		var unreadNotifications = await db.Notifications.AnyAsync(p => p.Notifiee == user && !p.IsRead);

		return new StatusResponse { UnreadAnnouncements = unreadAnnouncements, UnreadNotifications = unreadNotifications };
	}
}