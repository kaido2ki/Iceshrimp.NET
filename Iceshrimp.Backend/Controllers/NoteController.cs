using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[Produces("application/json")]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/v1/note")]
public class NoteController(DatabaseContext db) : Controller
{
	[HttpGet("{id}")]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNote(string id)
	{
		var user = HttpContext.GetUser();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("User not found");

		return Ok(NoteRenderer.RenderOne(note.EnforceRenoteReplyVisibility()));
	}
}