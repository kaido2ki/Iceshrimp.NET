using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers;

[Authenticate]
[Authorize("role:admin")]
[ApiController]
[Route("/api/v1/iceshrimp/admin")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrorResponse))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrorResponse))]
[SuppressMessage("ReSharper", "SuggestBaseTypeForParameterInConstructor",
                 Justification = "We only have a DatabaseContext in our DI pool, not the base type")]
public class AdminController(DatabaseContext db) : Controller {
	[HttpPost("invites/generate")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InviteResponse))]
	public async Task<IActionResult> GenerateInvite() {
		var invite = new RegistrationTicket {
			Id        = IdHelpers.GenerateSlowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Code      = CryptographyHelpers.GenerateRandomString(32)
		};

		await db.AddAsync(invite);
		await db.SaveChangesAsync();

		var res = new InviteResponse {
			Code = invite.Code
		};

		return Ok(res);
	}
}