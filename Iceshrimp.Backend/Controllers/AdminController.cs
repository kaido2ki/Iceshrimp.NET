using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Federation;
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
	public async Task<IActionResult> FetchActivityAsync([FromQuery] string uri)
	{
		return Ok(LdHelpers.Compact(await fetchSvc.FetchActivityAsync(uri)));
	}

	[UseNewtonsoftJson]
	[HttpGet("activities/fetch-raw")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASObject))]
	[ProducesResponseType(StatusCodes.Status422UnprocessableEntity, Type = typeof(ErrorResponse))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task FetchRawActivityByUsername([FromQuery] string uri)
	{
		var activity = await fetchSvc.FetchRawActivityAsync(uri);
		if (activity == null) throw GracefulException.UnprocessableEntity("Failed to fetch activity");

		Response.ContentType = Request.Headers.Accept.Any(p => p != null && p.StartsWith("application/ld+json"))
			? "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
			: "application/activity+json";

		await Response.WriteAsync(activity);
	}
}