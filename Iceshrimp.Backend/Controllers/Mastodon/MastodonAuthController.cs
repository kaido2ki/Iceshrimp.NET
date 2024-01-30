using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[ApiController]
[Tags("Mastodon")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
[Route("/api/v1")]
public class MastodonAuthController(DatabaseContext db) : Controller {
	[HttpGet("verify_credentials")]
	[AuthenticateOauth]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MastodonAuth.VerifyCredentialsResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	public IActionResult VerifyCredentials() {
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");

		var res = new MastodonAuth.VerifyCredentialsResponse {
			App      = token.App,
			VapidKey = null //FIXME
		};

		return Ok(res);
	}

	[HttpPost("apps")]
	[EnableRateLimiting("strict")]
	[ConsumesHybrid]
	[Produces("application/json")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MastodonAuth.RegisterAppResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> RegisterApp([FromHybrid] MastodonAuth.RegisterAppRequest request) {
		if (request.RedirectUris.Count == 0)
			throw GracefulException.BadRequest("Invalid redirect_uris parameter");

		if (request.RedirectUris.Any(p => !MastodonOauthHelpers.ValidateRedirectUri(p)))
			throw GracefulException.BadRequest("redirect_uris parameter contains invalid protocols");

		if (!MastodonOauthHelpers.ValidateScopes(request.Scopes))
			throw GracefulException.BadRequest("Invalid scopes parameter");

		if (request.Website != null)
			try {
				var uri = new Uri(request.Website);
				if (!uri.IsAbsoluteUri || uri.Scheme is "http" or "https") throw new Exception();
			}
			catch {
				throw GracefulException.BadRequest("Invalid website URL");
			}

		var app = new OauthApp {
			Id           = IdHelpers.GenerateSlowflakeId(),
			ClientId     = CryptographyHelpers.GenerateRandomString(32),
			ClientSecret = CryptographyHelpers.GenerateRandomString(32),
			CreatedAt    = DateTime.UtcNow,
			Name         = request.ClientName,
			Website      = request.Website,
			Scopes       = request.Scopes,
			RedirectUris = request.RedirectUris
		};

		await db.AddAsync(app);
		await db.SaveChangesAsync();

		var res = new MastodonAuth.RegisterAppResponse {
			App      = app,
			VapidKey = null //FIXME
		};

		return Ok(res);
	}
}