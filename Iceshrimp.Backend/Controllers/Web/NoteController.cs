using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using AsyncKeyedLock;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/notes")]
[Produces(MediaTypeNames.Application.Json)]
public class NoteController(
	DatabaseContext db,
	NoteService noteSvc,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer,
	CacheService cache
) : ControllerBase
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

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
		                   .FilterHidden(user, db, false, false)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		return Ok(await noteRenderer.RenderOne(note.EnforceRenoteReplyVisibility(), user));
	}

	[HttpDelete("{id}")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> DeleteNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.NotFound("Note not found");

		await noteSvc.DeleteNoteAsync(note);
		return Ok();
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
		                   .FilterHidden(user, db, false, false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var notes = await db.NoteAncestors(note, limit ?? 20)
		                    .Include(p => p.User.UserProfile)
		                    .Include(p => p.Renote!.User.UserProfile)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .PrecomputeNoteContextVisibilities(user)
		                    .ToListAsync();
		var res = await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user,
		                                        Filter.FilterContext.Threads);

		return Ok(res.ToList().OrderAncestors());
	}

	[HttpGet("{id}/descendants")]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NoteResponse>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> GetNoteDescendants(
		string id, [FromQuery] [DefaultValue(20)] [Range(1, 100)] int? depth
	)
	{
		var user = HttpContext.GetUser();

		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db, false, false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var hits = await db.NoteDescendants(note, depth ?? 20, 100)
		                   .Include(p => p.User.UserProfile)
		                   .Include(p => p.Renote!.User.UserProfile)
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db)
		                   .PrecomputeNoteContextVisibilities(user)
		                   .ToListAsync();

		var notes = hits.EnforceRenoteReplyVisibility();
		var res   = await noteRenderer.RenderMany(notes, user, Filter.FilterContext.Threads);
		return Ok(res.ToList().OrderDescendants());
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
		var note = await db.Notes
		                   .Where(p => p.Id == id)
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
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
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
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var success = await noteSvc.UnlikeNoteAsync(note, user);

		return Ok(new ValueResponse(success ? --note.LikeCount : note.LikeCount));
	}

	[HttpPost("{id}/renote")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> RenoteNote(string id, [FromQuery] NoteVisibility? visibility = null)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var success = await noteSvc.RenoteNoteAsync(note, user, (Note.NoteVisibility?)visibility);
		return Ok(new ValueResponse(success != null ? ++note.RenoteCount : note.RenoteCount));
	}

	[HttpPost("{id}/unrenote")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> UnrenoteNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		var count = await noteSvc.UnrenoteNoteAsync(note, user);
		return Ok(new ValueResponse(note.RenoteCount - count));
	}

	[HttpPost("{id}/react/{name}")]
	[Authenticate]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ValueResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> ReactToNote(string id, string name)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
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

	[HttpPost("{id}/refetch")]
	[Authenticate]
	[Authorize]
	[EnableRateLimiting("strict")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteRefetchResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> RefetchNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id && p.User.Host != null && p.Uri != null)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db, false, false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.NotFound("Note not found");

		if (note.Uri == null)
			throw new Exception("note.Uri must not be null at this point");

		var errors = new List<string>();

		try
		{
			await noteSvc.ResolveNoteAsync(note.Uri, null, user, clearHistory: true, forceRefresh: true);
		}
		catch (Exception e)
		{
			errors.Add($"Failed to refetch note: {e.Message}");
		}

		if (note.ReplyUri != null)
		{
			try
			{
				await noteSvc.ResolveNoteAsync(note.ReplyUri, null, user, clearHistory: true, forceRefresh: true);
			}
			catch (Exception e)
			{
				errors.Add($"Failed to fetch reply target: {e.Message}");
			}
		}

		if (note.RenoteUri != null)
		{
			try
			{
				await noteSvc.ResolveNoteAsync(note.RenoteUri, null, user, clearHistory: true, forceRefresh: true);
			}
			catch (Exception e)
			{
				errors.Add($"Failed to fetch renote target: {e.Message}");
			}
		}

		db.ChangeTracker.Clear();
		note = await db.Notes.Where(p => p.Id == id && p.User.Host != null && p.Uri != null)
		               .IncludeCommonProperties()
		               .EnsureVisibleFor(user)
		               .FilterHidden(user, db, false, false)
		               .PrecomputeVisibilities(user)
		               .FirstOrDefaultAsync() ??
		       throw new Exception("Note disappeared during refetch");

		var res = new NoteRefetchResponse
		{
			Note = await noteRenderer.RenderOne(note.EnforceRenoteReplyVisibility(), user), Errors = errors
		};
		return Ok(res);
	}

	[HttpPost]
	[Authenticate]
	[Authorize]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NoteResponse))]
	public async Task<IActionResult> CreateNote(NoteCreateRequest request)
	{
		var user = HttpContext.GetUserOrFail();

		if (request.IdempotencyKey != null)
		{
			var    key = $"idempotency:{user.Id}:{request.IdempotencyKey}";
			string hit;
			using (await KeyedLocker.LockAsync(key))
			{
				hit = await cache.FetchAsync(key, TimeSpan.FromHours(24), () => $"_:{HttpContext.TraceIdentifier}");
			}

			if (hit != $"_:{HttpContext.TraceIdentifier}")
			{
				for (var i = 0; i <= 10; i++)
				{
					if (!hit.StartsWith('_')) break;
					await Task.Delay(100);
					hit = await cache.GetAsync<string>(key) ??
					      throw new Exception("Idempotency key status disappeared in for loop");
					if (i >= 10)
						throw GracefulException.RequestTimeout("Failed to resolve idempotency key note within 1000 ms");
				}

				return await GetNote(hit);
			}
		}

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

		var attachments = request.MediaIds != null
			? await db.DriveFiles.Where(p => request.MediaIds.Contains(p.Id)).ToListAsync()
			: null;

		var note = await noteSvc.CreateNoteAsync(user, (Note.NoteVisibility)request.Visibility, request.Text,
		                                         request.Cw, reply, renote, attachments);

		if (request.IdempotencyKey != null)
			await cache.SetAsync($"idempotency:{user.Id}:{request.IdempotencyKey}", note.Id, TimeSpan.FromHours(24));

		return Ok(await noteRenderer.RenderOne(note, user));
	}
}