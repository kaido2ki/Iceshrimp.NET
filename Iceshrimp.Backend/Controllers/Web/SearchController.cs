using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Iceshrimp.Backend.Core.Federation.ActivityPub.UserResolver;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/search")]
[Produces(MediaTypeNames.Application.Json)]
public class SearchController(
	DatabaseContext db,
	NoteService noteSvc,
	NoteRenderer noteRenderer,
	UserRenderer userRenderer,
	ActivityPub.UserResolver userResolver,
	IOptions<Config.InstanceSection> config
) : ControllerBase
{
	[HttpGet("notes")]
	[LinkPagination(20, 80)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NoteResponse>> SearchNotes(
		[FromQuery(Name = "q")] string query, PaginationQuery pagination
	)
	{
		var user = HttpContext.GetUserOrFail();
		var notes = await db.Notes
		                    .IncludeCommonProperties()
		                    .FilterByFtsQuery(query, user, config.Value, db)
		                    .EnsureVisibleFor(user)
		                    .FilterHidden(user, db)
		                    .Paginate(pagination, ControllerContext)
		                    .PrecomputeVisibilities(user)
		                    .ToListAsync();

		return await noteRenderer.RenderMany(notes.EnforceRenoteReplyVisibility(), user);
	}

	[HttpGet("users")]
	[LinkPagination(20, 80)]
	[ProducesResults(HttpStatusCode.OK)]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall",
	                 Justification = "Inspection doesn't know about the Projectable attribute")]
	public async Task<IEnumerable<UserResponse>> SearchUsers(
		[FromQuery(Name = "q")] string query, PaginationQuery pagination
	)
	{
		var users = await db.Users
		                    .IncludeCommonProperties()
		                    .Where(p => p.DisplayNameOrUsernameOrFqnContainsCaseInsensitive(query,
				                            config.Value.AccountDomain))
		                    .Paginate(pagination, ControllerContext) //TODO: this will mess up our sorting
		                    .OrderByDescending(p => p.NotesCount)
		                    .ToListAsync();

		return await userRenderer.RenderMany(users);
	}

	[HttpGet("lookup")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task<RedirectResponse> Lookup([FromQuery(Name = "target")] string target)
	{
		target = target.Trim();

		var instancePrefix = $"https://{config.Value.WebDomain}";
		var notePrefix     = $"{instancePrefix}/notes/";
		var userPrefix     = $"{instancePrefix}/users/";
		var userPrefixAlt  = $"{instancePrefix}/@";

		if (target.StartsWith('@') || target.StartsWith(userPrefixAlt))
		{
			var hit = await userResolver.ResolveAsync(target, SearchFlags);
			return new RedirectResponse { TargetUrl = $"/users/{hit.Id}" };
		}

		if (target.StartsWith("https://"))
		{
			Note? noteHit = null;
			User? userHit = null;
			if (target.StartsWith(notePrefix))
			{
				noteHit = await db.Notes.FirstOrDefaultAsync(p => p.Id == target.Substring(notePrefix.Length));
				if (noteHit == null)
					throw GracefulException.NotFound("No result found");
			}
			else if (target.StartsWith(userPrefix))
			{
				userHit = await db.Users.FirstOrDefaultAsync(p => p.Id == target.Substring(userPrefix.Length));
				if (userHit == null)
					throw GracefulException.NotFound("No result found");
			}
			else if (target.StartsWith(instancePrefix))
			{
				if (userHit == null)
					throw GracefulException.NotFound("No result found");
			}

			noteHit ??= await db.Notes.FirstOrDefaultAsync(p => p.Uri == target || p.Url == target);
			if (noteHit != null) return new RedirectResponse { TargetUrl = $"/notes/{noteHit.Id}" };

			userHit ??= await db.Users.FirstOrDefaultAsync(p => p.Uri == target ||
			                                                    (p.UserProfile != null &&
			                                                     p.UserProfile.Url == target));
			if (userHit != null) return new RedirectResponse { TargetUrl = $"/users/{userHit.Id}" };

			noteHit = await noteSvc.ResolveNoteAsync(target);
			if (noteHit != null) return new RedirectResponse { TargetUrl = $"/notes/{noteHit.Id}" };

			userHit = await userResolver.ResolveOrNullAsync(target, ResolveFlags.Uri | ResolveFlags.MatchUrl);
			if (userHit != null) return new RedirectResponse { TargetUrl = $"/users/{userHit.Id}" };

			throw GracefulException.NotFound("No result found");
		}

		throw GracefulException.BadRequest("Invalid lookup target");
	}
}