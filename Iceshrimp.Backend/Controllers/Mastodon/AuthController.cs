using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(DatabaseContext db) : ControllerBase
{
	[HttpGet("/api/v1/apps/verify_credentials")]
	[Authenticate]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthSchemas.VerifyAppCredentialsResponse))]
	[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(MastodonErrorResponse))]
	public IActionResult VerifyAppCredentials()
	{
		var token = HttpContext.GetOauthToken();
		if (token == null) throw GracefulException.Unauthorized("The access token is invalid");

		var res = new AuthSchemas.VerifyAppCredentialsResponse
		{
			App = token.App, VapidKey = Constants.VapidPublicKey
		};

		return Ok(res);
	}

	[HttpPost("/api/v1/apps")]
	[EnableRateLimiting("strict")]
	[ConsumesHybrid]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthSchemas.RegisterAppResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> RegisterApp([FromHybrid] AuthSchemas.RegisterAppRequest request)
	{
		if (request.RedirectUris.Count == 0)
			throw GracefulException.BadRequest("Invalid redirect_uris parameter");

		if (request.RedirectUris.Any(p => !MastodonOauthHelpers.ValidateRedirectUri(p)))
			throw GracefulException.BadRequest("redirect_uris parameter contains invalid protocols");

		if (!MastodonOauthHelpers.ValidateScopes(request.Scopes))
			throw GracefulException.BadRequest("Invalid scopes parameter");

		if (request.Website != null)
			try
			{
				var uri = new Uri(request.Website);
				if (!uri.IsAbsoluteUri || uri.Scheme is not "http" and not "https") throw new Exception();
			}
			catch
			{
				throw GracefulException.BadRequest("Invalid website URL");
			}

		var app = new OauthApp
		{
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

		var res = new AuthSchemas.RegisterAppResponse
		{
			App = app, VapidKey = Constants.VapidPublicKey
		};

		return Ok(res);
	}

	[HttpPost("/oauth/token")]
	[ConsumesHybrid]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AuthSchemas.OauthTokenResponse))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetOauthToken([FromHybrid] AuthSchemas.OauthTokenRequest request)
	{
		//TODO: app-level access (grant_type = "client_credentials")
		if (request.GrantType != "authorization_code")
			throw GracefulException.BadRequest("Invalid grant_type");
		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Code == request.Code &&
		                                                          p.App.ClientId == request.ClientId &&
		                                                          p.App.ClientSecret == request.ClientSecret);
		if (token == null)
			throw GracefulException
				.Unauthorized("Client authentication failed due to unknown client, no client authentication included, or unsupported authentication method.");

		if (token.Active)
			throw GracefulException
				.BadRequest("The provided authorization grant is invalid, expired, revoked, does not match the redirection URI used in the authorization request, or was issued to another client.");

		if (MastodonOauthHelpers.ExpandScopes(request.Scopes ?? [])
		                        .Except(MastodonOauthHelpers.ExpandScopes(token.Scopes))
		                        .Any())
			throw GracefulException.BadRequest("The requested scope is invalid, unknown, or malformed.");

		token.Scopes = request.Scopes ?? token.Scopes;
		token.Active = true;
		await db.SaveChangesAsync();

		var res = new AuthSchemas.OauthTokenResponse
		{
			CreatedAt = token.CreatedAt, Scopes = token.Scopes, AccessToken = token.Token
		};

		return Ok(res);
	}

	[HttpPost("/oauth/revoke")]
	[ConsumesHybrid]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(MastodonErrorResponse))]
	[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> RevokeOauthToken([FromHybrid] AuthSchemas.OauthTokenRevocationRequest request)
	{
		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Token == request.Token &&
		                                                          p.App.ClientId == request.ClientId &&
		                                                          p.App.ClientSecret == request.ClientSecret);
		if (token == null)
			throw GracefulException.Forbidden("You are not authorized to revoke this token");

		db.Remove(token);
		await db.SaveChangesAsync();

		return Ok(new object());
	}
}