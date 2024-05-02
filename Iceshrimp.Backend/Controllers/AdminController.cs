using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Federation;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers;

[Authenticate]
[Authorize("role:admin")]
[ApiController]
[Route("/api/iceshrimp/admin")]
[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We only have a DatabaseContext in our DI pool, not the base type")]
public class AdminController(
	DatabaseContext db,
	ActivityPubController apController,
	ActivityPub.ActivityFetcherService fetchSvc
) : ControllerBase
{
	[HttpPost("invites/generate")]
	[Produces(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InviteResponse))]
	public async Task<IActionResult> GenerateInvite()
	{
		var invite = new RegistrationInvite
		{
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Code      = CryptographyHelpers.GenerateRandomString(32)
		};

		await db.AddAsync(invite);
		await db.SaveChangesAsync();

		var res = new InviteResponse { Code = invite.Code };

		return Ok(res);
	}

	[HttpPost("users/{id}/reset-password")]
	[Produces(MediaTypeNames.Application.Json)]
	[Consumes(MediaTypeNames.Application.Json)]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
	{
		var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id && p.UserHost == null) ??
		              throw GracefulException.RecordNotFound();

		if (request.Password.Length < 8)
			throw GracefulException.BadRequest("Password must be at least 8 characters long");

		profile.Password = AuthHelpers.HashPassword(request.Password);
		await db.SaveChangesAsync();

		return Ok();
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/notes/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASNote))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> GetNoteActivity(string id, [FromServices] ActivityPub.NoteRenderer noteRenderer)
	{
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id && p.UserHost == null);
		if (note == null) return NotFound();
		var rendered  = await noteRenderer.RenderAsync(note);
		var compacted = LdHelpers.Compact(rendered);
		return Ok(compacted);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/notes/{id}/activity")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASAnnounce))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage")]
	[SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery")]
	public async Task<IActionResult> GetRenoteActivity(
		string id, [FromServices] ActivityPub.NoteRenderer noteRenderer,
		[FromServices] ActivityPub.UserRenderer userRenderer, [FromServices] IOptions<Config.InstanceSection> config
	)
	{
		var note = await db.Notes
		                   .IncludeCommonProperties()
		                   .FirstOrDefaultAsync(p => p.Id == id && p.UserHost == null);
		if (note is not { IsPureRenote: true, Renote: not null }) return NotFound();
		var rendered = ActivityPub.ActivityRenderer.RenderAnnounce(noteRenderer.RenderLite(note.Renote),
		                                                           note.GetPublicUri(config.Value),
		                                                           userRenderer.RenderLite(note.User),
		                                                           note.Visibility,
		                                                           note.User.GetPublicUri(config.Value) + "/followers");
		var compacted = rendered.Compact();
		return Ok(compacted);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> GetUserActivity(string id)
	{
		return await apController.GetUser(id);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/{id}/collections/featured")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> GetUserFeaturedActivity(string id)
	{
		return await apController.GetUserFeatured(id);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/users/@{acct}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> GetUserActivityByUsername(string acct)
	{
		return await apController.GetUserByUsername(acct);
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/fetch")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASObject))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> FetchActivityAsync([FromQuery] string uri, [FromQuery] string? userId)
	{
		var user = userId != null ? await db.Users.FirstOrDefaultAsync(p => p.Id == userId && p.Host == null) : null;
		var activity = await fetchSvc.FetchActivityAsync(uri, user);
		if (!activity.Any()) throw GracefulException.UnprocessableEntity("Failed to fetch activity");
		return Ok(LdHelpers.Compact(activity));
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/fetch-raw")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASObject))]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task FetchRawActivityAsync([FromQuery] string uri, [FromQuery] string? userId)
	{
		var user = userId != null ? await db.Users.FirstOrDefaultAsync(p => p.Id == userId && p.Host == null) : null;
		var activity = await fetchSvc.FetchRawActivityAsync(uri, user);
		if (activity == null) throw GracefulException.UnprocessableEntity("Failed to fetch activity");

		Response.ContentType = Request.Headers.Accept.Any(p => p != null && p.StartsWith("application/ld+json"))
			? "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
			: "application/activity+json";

		await Response.WriteAsync(activity);
	}
}