using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/statuses")]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class MastodonStatusController(DatabaseContext db, NoteRenderer noteRenderer) : Controller {
	[HttpGet("{id}")]
	[Authenticate("read:statuses")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Account))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetNote(string id) {
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.RecordNotFound();
		var res = await noteRenderer.RenderAsync(note);
		return Ok(res);
	}
}