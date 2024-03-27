using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
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
[Route("/api/iceshrimp/note")]
[Produces(MediaTypeNames.Application.Json)]
public class NoteController(
	DatabaseContext db,
	NoteService noteSvc,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer
) : ControllerBase
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
		                   .FilterIncomingBlocks(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		return Ok(await noteRenderer.RenderOne(note.EnforceRenoteReplyVisibility(), user));
	}

	[HttpGet("{id}/ascendants")]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNoteAscendants(
		string id, [FromQuery] [DefaultValue(20)] [Range(1, 100)] int? limit
	)
	{
		var user = HttpContext.GetUser();

		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FilterIncomingBlocks(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var notes = await db.NoteAncestors(note, limit ?? 20)
		                    .Include(p => p.User.UserProfile)
		                    .Include(p => p.Renote!.User.UserProfile)
		                    .EnsureVisibleFor(user)
		                    .FilterBlocked(user)
		                    .FilterMuted(user)
		                    .PrecomputeNoteContextVisibilities(user)
		                    .ToListAsync();

		return Ok(await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user));
	}

	[HttpGet("{id}/descendants")]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNoteDescendants(
		string id, [FromQuery] [DefaultValue(20)] [Range(1, 100)] int? depth
	)
	{
		var user = HttpContext.GetUser();

		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FilterIncomingBlocks(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var notes = await db.NoteDescendants(note, depth ?? 20, 100)
		                    .Include(p => p.User.UserProfile)
		                    .Include(p => p.Renote!.User.UserProfile)
		                    .EnsureVisibleFor(user)
		                    .FilterBlocked(user)
		                    .FilterMuted(user)
		                    .PrecomputeNoteContextVisibilities(user)
		                    .ToListAsync();

		return Ok(await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user));
	}

	[HttpGet("{id}/reactions/{name}")]
	[Authenticate]
	[Authorize]
	[LinkPagination(20, 40)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNoteReactions(string id, string name)
	{
		var user = HttpContext.GetUser();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var users = await db.NoteReactions
		                    .Where(p => p.Note == note && p.Reaction == $":{name.Trim(':')}:")
		                    .Include(p => p.User.UserProfile)
		                    .Select(p => p.User)
		                    .ToListAsync();

		return Ok(await userRenderer.RenderMany(users));
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

	[HttpPost("{id}/react/{name}")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> ReactToNote(string id, string name)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var res = await noteSvc.ReactToNoteAsync(note, user, name);
		note.Reactions.TryGetValue(res.name, out var count);
		return Ok(new ValueResponse(res.success ? ++count : count));
	}

	[HttpPost("{id}/unreact/{name}")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> RemoveReactionFromNote(string id, string name)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var res = await noteSvc.RemoveReactionFromNoteAsync(note, user, name);
		note.Reactions.TryGetValue(res.name, out var count);
		return Ok(new ValueResponse(res.success ? --count : count));
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

		return Ok(await noteRenderer.RenderOne(note, user));
	}
}