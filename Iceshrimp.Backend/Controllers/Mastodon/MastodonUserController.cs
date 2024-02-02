using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[ApiController]
[Tags("Mastodon")]
[Route("/api/v1/accounts")]
[AuthenticateOauth]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class MastodonUserController(UserRenderer userRenderer) : Controller {
	[AuthorizeOauth("read:accounts")]
	[HttpGet("verify_credentials")]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Account))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> VerifyUserCredentials() {
		var user = HttpContext.GetOauthUser() ?? throw new GracefulException("Failed to get user from HttpContext");
		var res  = await userRenderer.RenderAsync(user);
		return Ok(res);
	}
}