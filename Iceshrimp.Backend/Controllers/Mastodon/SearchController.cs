using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Iceshrimp.Backend.Controllers.Attributes;
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

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class SearchController(
	DatabaseContext db,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer,
	NoteService noteSvc,
	ActivityPub.UserResolver userResolver
) : Controller {
	[HttpGet("/api/v2/search")]
	[Authorize("read:search")]
	[LinkPagination(20, 40)]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SearchSchemas.SearchResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> Search(SearchSchemas.SearchRequest search, PaginationQuery pagination) {
		if (search.Query == null)
			throw GracefulException.BadRequest("Query is missing or invalid");

		var result = new SearchSchemas.SearchResponse {
			Accounts = search.Type is null or "accounts" ? await SearchUsersAsync(search, pagination) : [],
			Statuses = search.Type is null or "statuses" ? await SearchNotesAsync(search, pagination) : []
			//TODO: implement hashtags
		};

		return Ok(result);
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Inspection doesn't know about the Projectable attribute")]
	private async Task<List<Account>> SearchUsersAsync(SearchSchemas.SearchRequest search, PaginationQuery pagination) {
		var user = HttpContext.GetUserOrFail();

		if (search.Resolve) {
			if (search.Query!.StartsWith("https://") || search.Query.StartsWith("http://")) {
				try {
					var result = await userResolver.ResolveAsync(search.Query);
					return [await userRenderer.RenderAsync(result)];
				}
				catch {
					return [];
				}
			}
			else {
				var regex = new Regex
					("^(?:@?(?<user>[a-zA-Z0-9_]+)@(?<host>[a-zA-Z0-9-.]+\\.[a-zA-Z0-9-]+))|(?:@(?<localuser>[a-zA-Z0-9_]+))$");
				var match = regex.Match(search.Query);
				if (match.Success) {
					if (match.Groups["localuser"].Success) {
						var username = match.Groups["localuser"].Value.ToLowerInvariant();
						var result = await db.Users
						                     .IncludeCommonProperties()
						                     .Where(p => p.UsernameLower == username)
						                     .FirstOrDefaultAsync();

						return result != null ? [await userRenderer.RenderAsync(result)] : [];
					}
					else {
						var username = match.Groups["user"].Value;
						var host     = match.Groups["host"].Value;

						try {
							var result = await userResolver.ResolveAsync($"@{username}@{host}");
							return [await userRenderer.RenderAsync(result)];
						}
						catch {
							return [];
						}
					}
				}
			}
		}

		return await db.Users
		               .IncludeCommonProperties()
		               .Where(p => p.DisplayNameContainsCaseInsensitive(search.Query!) ||
		                           p.UsernameContainsCaseInsensitive(search.Query!))
		               .Where(p => !search.Following || p.IsFollowedBy(user))
		               .Paginate(pagination, ControllerContext) //TODO: this will mess up our sorting
		               .OrderByDescending(p => p.NotesCount)
		               .Skip(pagination.Offset ?? 0)
		               .RenderAllForMastodonAsync(userRenderer);
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Inspection doesn't know about the Projectable attribute")]
	private async Task<List<Status>> SearchNotesAsync(SearchSchemas.SearchRequest search, PaginationQuery pagination) {
		var user = HttpContext.GetUserOrFail();

		if (search.Resolve && (search.Query!.StartsWith("https://") || search.Query.StartsWith("http://"))) {
			var note = await db.Notes
			                   .IncludeCommonProperties()
			                   .Where(p => p.Uri == search.Query || p.Url == search.Query)
			                   .EnsureVisibleFor(user)
			                   .PrecomputeVisibilities(user)
			                   .FirstOrDefaultAsync();

			// Second check in case note is known but visibility prevents us from returning it
			//TODO: figure out whether there is a more efficient way to get this information (in one query)
			if (note == null && !await db.Notes.AnyAsync(p => p.Uri == search.Query || p.Url == search.Query)) {
				var tmpNote = await noteSvc.ResolveNoteAsync(search.Query);

				// We need to re-fetch the note to capture the includes
				if (tmpNote != null) {
					note = await db.Notes
					               .IncludeCommonProperties()
					               .Where(p => p.Id == tmpNote.Id)
					               .EnsureVisibleFor(user)
					               .PrecomputeVisibilities(user)
					               .FirstOrDefaultAsync();
				}
			}

			return note != null ? [await noteRenderer.RenderAsync(note, user)] : [];
		}

		return await db.Notes
		               .IncludeCommonProperties()
		               .Where(p => p.TextContainsCaseInsensitive(search.Query!) &&
		                           (!search.Following || p.User.IsFollowedBy(user)))
		               .FilterByUser(search.UserId)
		               .EnsureVisibleFor(user)
		               .FilterHiddenListMembers(user)
		               .FilterBlocked(user)
		               .FilterMuted(user)
		               .Paginate(pagination, ControllerContext)
		               .Skip(pagination.Offset ?? 0)
		               .PrecomputeVisibilities(user)
		               .RenderAllForMastodonAsync(noteRenderer, user);
	}
}