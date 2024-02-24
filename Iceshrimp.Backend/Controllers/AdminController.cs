using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers;

[Authenticate]
[Authorize("role:admin")]
[ApiController]
[Route("/api/v1/iceshrimp/admin")]
[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We only have a DatabaseContext in our DI pool, not the base type")]
public class AdminController(DatabaseContext db, ActivityPubController apController) : ControllerBase
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
	public async Task<IActionResult> GetNoteActivity(string id)
	{
		return await apController.GetNote(id);
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
	[HttpGet("activities/users/@{acct}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ASActor))]
	[Produces("application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"")]
	public async Task<IActionResult> GetUserActivityByUsername(string acct)
	{
		return await apController.GetUserByUsername(acct);
	}
}