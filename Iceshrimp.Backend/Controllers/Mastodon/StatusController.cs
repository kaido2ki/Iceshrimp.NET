using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/statuses")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class StatusController(
	DatabaseContext db,
	NoteRenderer noteRenderer,
	NoteService noteSvc,
	IDistributedCache cache,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet("{id}")]
	[Authenticate("read:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetNote(string id)
	{
		var user = HttpContext.GetUser();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();
		var res = await noteRenderer.RenderAsync(note.EnforceRenoteReplyVisibility(), user);
		return Ok(res);
	}

	[HttpGet("{id}/context")]
	[Authenticate("read:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetStatusContext(string id)
	{
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
		                          .Where(p => !p.IsQuote || p.RenoteId != id)
		                          .IncludeCommonProperties()
		                          .EnsureVisibleFor(user)
		                          .PrecomputeVisibilities(user)
		                          .RenderAllForMastodonAsync(noteRenderer, user);

		var res = new StatusContext { Ancestors = ancestors, Descendants = descendants };

		return Ok(res);
	}

	[HttpPost("{id}/favourite")]
	[Authorize("write:favourites")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> LikeNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		await noteSvc.LikeNoteAsync(note, user);
		return await GetNote(id);
	}

	[HttpPost("{id}/unfavourite")]
	[Authorize("write:favourites")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UnlikeNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		await noteSvc.UnlikeNoteAsync(note, user);
		return await GetNote(id);
	}

	[HttpPost("{id}/reblog")]
	[Authorize("write:favourites")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> Renote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		await noteSvc.CreateNoteAsync(user, Note.NoteVisibility.Followers, renote: note);
		return await GetNote(id);
	}

	[HttpPost("{id}/unreblog")]
	[Authorize("write:favourites")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> UndoRenote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		if (!await db.Notes.Where(p => p.Id == id).EnsureVisibleFor(user).AnyAsync())
			throw GracefulException.RecordNotFound();

		var renotes = await db.Notes.Where(p => p.RenoteId == id && p.IsPureRenote && p.User == user)
		                      .IncludeCommonProperties()
		                      .ToListAsync();

		if (renotes.Count > 0)
		{
			renotes[0].Renote!.RenoteCount--;
			await db.SaveChangesAsync();
		}

		foreach (var renote in renotes) await noteSvc.DeleteNoteAsync(renote);

		return await GetNote(id);
	}

	[HttpPost]
	[Authorize("write:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> PostNote([FromHybrid] StatusSchemas.PostStatusRequest request)
	{
		var user = HttpContext.GetUserOrFail();

		//TODO: handle scheduled statuses
		Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyHeader);
		var idempotencyKey = idempotencyKeyHeader.FirstOrDefault();
		if (idempotencyKey != null)
		{
			var hit = await cache.FetchAsync($"idempotency:{idempotencyKey}", TimeSpan.FromHours(24),
			                                 () => $"_:{HttpContext.TraceIdentifier}");

			if (hit != $"_:{HttpContext.TraceIdentifier}")
			{
				for (var i = 0; i <= 10; i++)
				{
					if (!hit.StartsWith('_')) break;
					await Task.Delay(100);
					hit = await cache.GetAsync<string>($"idempotency:{idempotencyKey}") ??
					      throw new Exception("Idempotency key status disappeared in for loop");
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

		var visibility = StatusEntity.DecodeVisibility(request.Visibility);
		var reply = request.ReplyId != null
			? await db.Notes.Where(p => p.Id == request.ReplyId)
			          .IncludeCommonProperties()
			          .EnsureVisibleFor(user)
			          .FirstOrDefaultAsync() ??
			  throw GracefulException.BadRequest("Reply target is nonexistent or inaccessible")
			: null;

		var attachments = request.MediaIds != null
			? await db.DriveFiles.Where(p => request.MediaIds.Contains(p.Id)).ToListAsync()
			: null;

		var lastToken = request.Text?.Split(' ').LastOrDefault();
		var quoteUri  = lastToken?.StartsWith("https://") ?? false ? lastToken : null;
		var quote = lastToken?.StartsWith($"https://{config.Value.WebDomain}/notes/") ?? false
			? await db.Notes.IncludeCommonProperties()
			          .FirstOrDefaultAsync(p => p.Id ==
			                                    lastToken.Substring($"https://{config.Value.WebDomain}/notes/".Length))
			: await db.Notes.IncludeCommonProperties()
			          .FirstOrDefaultAsync(p => p.Uri == quoteUri || p.Url == quoteUri);

		if (quote != null && quoteUri != null && request.Text != null)
			request.Text = request.Text[..(request.Text.Length - quoteUri.Length - 1)];

		var note = await noteSvc.CreateNoteAsync(user, visibility, request.Text, request.Cw, reply, quote, attachments);

		if (idempotencyKey != null)
			await cache.SetAsync($"idempotency:{idempotencyKey}", note.Id, TimeSpan.FromHours(24));

		var res = await noteRenderer.RenderAsync(note, user);

		return Ok(res);
	}

	[HttpPut("{id}")]
	[Authorize("write:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> EditNote(string id, [FromHybrid] StatusSchemas.EditStatusRequest request)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();

		if (request.Text == null && request.MediaIds is not { Count: > 0 } && request.Poll == null)
			throw GracefulException.BadRequest("Posts must have text, media or poll");

		if (request.Poll != null)
			throw GracefulException.BadRequest("Polls haven't been implemented yet");

		var attachments = request.MediaIds != null
			? await db.DriveFiles.Where(p => request.MediaIds.Contains(p.Id)).ToListAsync()
			: [];

		note = await noteSvc.UpdateNoteAsync(note, request.Text, request.Cw, attachments);
		var res = await noteRenderer.RenderAsync(note, user);

		return Ok(res);
	}

	[HttpDelete("{id}")]
	[Authorize("write:statuses")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusEntity))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> DeleteNote(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id && p.User == user) ??
		           throw GracefulException.RecordNotFound();
		if (user.Id != note.UserId)
			throw GracefulException.RecordNotFound();

		var res = await noteRenderer.RenderAsync(note, user);
		await noteSvc.DeleteNoteAsync(note);

		return Ok(res);
	}
}