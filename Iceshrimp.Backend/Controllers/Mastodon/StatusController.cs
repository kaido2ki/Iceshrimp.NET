using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/statuses")]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class StatusController(DatabaseContext db, NoteRenderer noteRenderer, NoteService noteSvc) : Controller {
	[HttpGet("{id}")]
	[Authenticate("read:statuses")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetNote(string id) {
		var user = HttpContext.GetUser();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync()
		           ?? throw GracefulException.RecordNotFound();
		var res = await noteRenderer.RenderAsync(note.EnforceRenoteReplyVisibility());
		return Ok(res);
	}

	[HttpPost]
	[Authorize("write:statuses")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> PostNote([FromHybrid] StatusSchemas.PostStatusRequest request) {
		var user = HttpContext.GetUserOrFail();

		//TODO: handle scheduled statuses
		//TODO: handle Idempotency-Key

		if (request.Text == null)
			throw GracefulException.BadRequest("Posts without text haven't been implemented yet");

		var visibility = Status.DecodeVisibility(request.Visibility);
		var reply = request.ReplyId != null
			? await db.Notes.Where(p => p.Id == request.ReplyId).EnsureVisibleFor(user).FirstOrDefaultAsync() ??
			  throw GracefulException.BadRequest("Reply target is nonexistent or inaccessible")
			: null;

		var note = await noteSvc.CreateNoteAsync(user, visibility, request.Text, request.Cw, reply);
		var res  = await noteRenderer.RenderAsync(note);

		return Ok(res);
	}
}