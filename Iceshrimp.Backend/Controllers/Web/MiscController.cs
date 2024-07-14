using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
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
public class MiscController(DatabaseContext db, NoteRenderer noteRenderer) : ControllerBase
{
	[HttpGet("muted_threads")]
	[LinkPagination(20, 40)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> GetMutedThreads(PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();
		var notes = await db.Notes.IncludeCommonProperties()
		                    .Where(p => db.NoteThreadMutings.Any(m => m.ThreadId == (p.ThreadId ?? p.Id)))
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db, false, false)
		                    .Paginate(pq, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user);
	}
}