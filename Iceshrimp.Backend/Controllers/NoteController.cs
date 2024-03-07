using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/v1/note")]
[Produces(MediaTypeNames.Application.Json)]
public class NoteController(DatabaseContext db, NoteService noteSvc, NoteRenderer noteRenderer) : ControllerBase
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
		           throw GracefulException.NotFound("Note not found");

		return Ok(await noteRenderer.RenderOne(note.EnforceRenoteReplyVisibility()));
	}

	[HttpPost("{id}/like")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> LikeNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var success = await noteSvc.LikeNoteAsync(note, user);

		return Ok(new ValueResponse(success ? ++note.LikeCount : note.LikeCount));
	}

	[HttpPost("{id}/unlike")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UnlikeNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var success = await noteSvc.UnlikeNoteAsync(note, user);

		return Ok(new ValueResponse(success ? --note.LikeCount : note.LikeCount));
	}

	[HttpPost]
	[Authenticate]
	[Authorize]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteResponse))]
	public async Task<IActionResult> CreateNote(NoteCreateRequest request)
	{
		var user = HttpContext.GetUserOrFail();

		var reply = request.ReplyId != null
			? await db.Notes.Where(p => p.Id == request.ReplyId)
			          .IncludeCommonProperties()
			          .EnsureVisibleFor(user)
			          .FirstOrDefaultAsync() ??
			  throw GracefulException.BadRequest("Reply target is nonexistent or inaccessible")
			: null;

		var renote = request.RenoteId != null
			? await db.Notes.Where(p => p.Id == request.RenoteId)
			          .IncludeCommonProperties()
			          .EnsureVisibleFor(user)
			          .FirstOrDefaultAsync() ??
			  throw GracefulException.BadRequest("Renote target is nonexistent or inaccessible")
			: null;

		var note = await noteSvc.CreateNoteAsync(user, Note.NoteVisibility.Public, request.Text, request.Cw, reply,
		                                         renote);

		return Ok(await noteRenderer.RenderOne(note));
	}
}