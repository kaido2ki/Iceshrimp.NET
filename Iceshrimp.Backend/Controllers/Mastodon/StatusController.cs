using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/statuses")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class StatusController(
	DatabaseContext db,
	NoteRenderer noteRenderer,
	NoteService noteSvc,
	IDistributedCache cache
) : Controller {
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
		var res = await noteRenderer.RenderAsync(note.EnforceRenoteReplyVisibility(), user);
		return Ok(res);
	}

	[HttpGet("{id}/context")]
	[Authenticate("read:statuses")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetStatusContext(string id) {
		var user           = HttpContext.GetUser();
		var maxAncestors   = user != null ? 4096 : 40;
		var maxDescendants = user != null ? 4096 : 60;
		var maxDepth       = user != null ? 4096 : 20;

		if (!db.Notes.Any(p => p.Id == id))
			throw GracefulException.RecordNotFound();

		var ancestors = await db.NoteAncestors(id, maxAncestors)
		                        .IncludeCommonProperties()
		                        .EnsureVisibleFor(user)
		                        .PrecomputeVisibilities(user)
		                        .RenderAllForMastodonAsync(noteRenderer, user);

		var descendants = await db.NoteDescendants(id, maxDepth, maxDescendants)
		                          .IncludeCommonProperties()
		                          .EnsureVisibleFor(user)
		                          .PrecomputeVisibilities(user)
		                          .RenderAllForMastodonAsync(noteRenderer, user);

		var res = new StatusContext {
			Ancestors   = ancestors,
			Descendants = descendants
		};

		return Ok(res);
	}

	[HttpPost("{id}/favourite")]
	[Authorize("write:favourites")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> LikeNote(string id) {
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync()
		           ?? throw GracefulException.RecordNotFound();

		await noteSvc.LikeNoteAsync(note, user);
		return await GetNote(id);
	}

	[HttpPost("{id}/unfavourite")]
	[Authorize("write:favourites")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UnlikeNote(string id) {
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync()
		           ?? throw GracefulException.RecordNotFound();

		await noteSvc.UnlikeNoteAsync(note, user);
		return await GetNote(id);
	}

	[HttpPost]
	[Authorize("write:statuses")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Status))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> PostNote([FromHybrid] StatusSchemas.PostStatusRequest request) {
		var user = HttpContext.GetUserOrFail();

		//TODO: handle scheduled statuses
		Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader);
		var idempotencyKey = idempotencyKeyHeader.FirstOrDefault();
		if (idempotencyKey != null) {
			var hit = await cache.FetchAsync($"idempotency:{idempotencyKey}", TimeSpan.FromHours(24),
			                                 () => $"_:{HttpContext.TraceIdentifier}");
			
			if (hit != $"_:{HttpContext.TraceIdentifier}") {
				for (var i = 0; i <= 10; i++) {
					if (!hit.StartsWith('_')) break;
					await Task.Delay(100);
					hit = await cache.GetAsync<string>($"idempotency:{idempotencyKey}")
					      ?? throw new Exception("Idempotency key status disappeared in for loop");
					if (i >= 10)
						throw GracefulException.RequestTimeout("Failed to resolve idempotency key note within 1000 ms");
				}

				return await GetNote(hit);
			}
		}

		if (request.Text == null && request.MediaIds is not { Count: > 0 } && request.Poll == null)
			throw GracefulException.BadRequest("Posts must have text, media or poll");

		if (request.Poll != null)
			throw GracefulException.BadRequest("Polls haven't been implemented yet");

		var visibility = Status.DecodeVisibility(request.Visibility);
		var reply = request.ReplyId != null
			? await db.Notes.Where(p => p.Id == request.ReplyId).EnsureVisibleFor(user).FirstOrDefaultAsync() ??
			  throw GracefulException.BadRequest("Reply target is nonexistent or inaccessible")
			: null;

		var attachments = request.MediaIds != null
			? await db.DriveFiles.Where(p => request.MediaIds.Contains(p.Id)).ToListAsync()
			: null;

		var note = await noteSvc.CreateNoteAsync(user, visibility, request.Text, request.Cw, reply,
		                                         attachments: attachments);
		
		if (idempotencyKey != null)
			await cache.SetAsync($"idempotency:{idempotencyKey}", note.Id, TimeSpan.FromHours(24));

		var res = await noteRenderer.RenderAsync(note, user);

		return Ok(res);
	}
}