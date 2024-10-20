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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/misc")]
[Produces(MediaTypeNames.Application.Json)]
public class MiscController(DatabaseContext db, NoteRenderer noteRenderer, BiteService biteSvc) : ControllerBase
{
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
	
	[HttpGet("muted_threads")]
	[LinkPagination(20, 40)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetMutedThreads(PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => db.NoteThreadMutings.Any(m => m.ThreadId == p.ThreadIdOrId))
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, false, false)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user);
	}
}