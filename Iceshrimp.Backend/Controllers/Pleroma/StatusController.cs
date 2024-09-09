using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
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
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[Route("/api/v1/pleroma/statuses")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class StatusController(
	DatabaseContext db,
	NoteRenderer noteRenderer,
	NoteService noteSvc,
	UserRenderer userRenderer,
	IOptionsSnapshot<Config.SecuritySection> security
) : ControllerBase
{
	private async Task<StatusEntity> GetNote(string id)
	{
		var user = HttpContext.GetUser();
		var note = await db.Notes
		                   .Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .FilterHidden(user, db, false, false,
		                                 filterMentions: false)
		                   .EnsureVisibleFor(user)
		                   .PrecomputeVisibilities(user)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();
		return await noteRenderer.RenderAsync(note.EnforceRenoteReplyVisibility(), user);
	}

	[HttpGet("{id}/reactions")]
	[Authenticate("read:statuses")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
	public async Task<IEnumerable<ReactionEntity>> GetNoteReactions(string id)
	{
		var user = HttpContext.GetUser();
		if (security.Value.PublicPreview == Enums.PublicPreview.Lockdown && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var note = await db.Notes.Where(p => p.Id == id)
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db, filterMutes: false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		if (security.Value.PublicPreview <= Enums.PublicPreview.Restricted && note.UserHost != null && user == null)
			throw GracefulException.Forbidden("Public preview is disabled on this instance");

		var res = await noteRenderer.GetReactions([note], user);

		foreach (var item in res)
		{
			if (item.AccountIds == null) continue;

			var accounts = await db.Users.Where(u => item.AccountIds.Contains(u.Id)).ToArrayAsync();
			item.Accounts = (await userRenderer.RenderManyAsync(accounts)).ToList();
		}

		return res;
	}

	[HttpPut("{id}/reactions/{reaction}")]
	[Authorize("write:favourites")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<StatusEntity> ReactNote(string id, string reaction)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db, filterMutes: false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var res = await noteSvc.ReactToNoteAsync(note, user, reaction);
		if (res.success && !note.Reactions.TryAdd(res.name, 1))
			note.Reactions[res.name]++; // we do not want to call save changes after this point

		return await GetNote(id);
	}

	[HttpDelete("{id}/reactions/{reaction}")]
	[Authorize("write:favourites")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<StatusEntity> UnreactNote(string id, string reaction)
	{
		var user = HttpContext.GetUserOrFail();
		var note = await db.Notes.Where(p => p.Id == id)
		                   .IncludeCommonProperties()
		                   .EnsureVisibleFor(user)
		                   .FilterHidden(user, db, filterMutes: false)
		                   .FirstOrDefaultAsync() ??
		           throw GracefulException.RecordNotFound();

		var res = await noteSvc.RemoveReactionFromNoteAsync(note, user, reaction);
		if (res.success && note.Reactions.TryGetValue(res.name, out var value))
			note.Reactions[res.name] = --value; // we do not want to call save changes after this point

		return await GetNote(id);
	}
}
