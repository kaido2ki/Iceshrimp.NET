using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/accounts")]
[Authenticate]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class MastodonAccountController(DatabaseContext db, UserRenderer userRenderer) : Controller {
	[Authorize("read:accounts")]
	[HttpGet("verify_credentials")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Account))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> VerifyUserCredentials() {
		var user = HttpContext.GetUser() ?? throw new GracefulException("Failed to get user from HttpContext");
		var res  = await userRenderer.RenderAsync(user);
		return Ok(res);
	}

	[HttpGet("{id}")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Account))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetUser(string id) {
		var user = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id)
		           ?? throw GracefulException.RecordNotFound();
		var res = await userRenderer.RenderAsync(user);
		return Ok(res);
	}
}