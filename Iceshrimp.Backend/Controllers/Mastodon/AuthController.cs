using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Backend.Controllers.Mastodon.Schemas.AuthSchemas;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[EnableRateLimiting("sliding")]
[EnableCors("mastodon")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(DatabaseContext db, MetaService meta) : ControllerBase
{
	[HttpGet("/api/v1/apps/verify_credentials")]
	[Authenticate]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.Unauthorized)]
	public async Task<VerifyAppCredentialsResponse> VerifyAppCredentials()
	{
		var token = HttpContext.GetOauthToken() ?? throw GracefulException.Unauthorized("The access token is invalid");

		return new VerifyAppCredentialsResponse
		{
			App = token.App, VapidKey = await meta.Get(MetaEntity.VapidPublicKey)
		};
	}

	[HttpPost("/api/v1/apps")]
	[EnableRateLimiting("auth")]
	[ConsumesHybrid]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<RegisterAppResponse> RegisterApp([FromHybrid] RegisterAppRequest request)
	{
		if (request.RedirectUris.Count == 0)
			throw GracefulException.BadRequest("Invalid redirect_uris parameter");

		if (request.RedirectUris.Any(p => !MastodonOauthHelpers.ValidateRedirectUri(p)))
			throw GracefulException.BadRequest("redirect_uris parameter contains invalid protocols");

		if (!MastodonOauthHelpers.ValidateScopes(request.Scopes))
			throw GracefulException.BadRequest("Invalid scopes parameter");

		if (!string.IsNullOrWhiteSpace(request.Website))
			try
			{
				var uri = new Uri(request.Website.Trim());
				if (!uri.IsAbsoluteUri || uri.Scheme is not "http" and not "https") throw new Exception();
			}
			catch
			{
				throw GracefulException.BadRequest("Invalid website URL");
			}

		var app = new OauthApp
		{
			Id           = IdHelpers.GenerateSnowflakeId(),
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

		return new RegisterAppResponse { App = app, VapidKey = await meta.Get(MetaEntity.VapidPublicKey) };
	}

	[HttpPost("/oauth/token")]
	[ConsumesHybrid]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<OauthTokenResponse> GetOauthToken([FromHybrid] OauthTokenRequest request)
	{
		if (request.GrantType == "client_credentials")
		{
			// @formatter:off
			var app = await db.OauthApps.FirstOrDefaultAsync(p => p.ClientId == request.ClientId &&
			                                                      p.ClientSecret == request.ClientSecret) ??
			          throw GracefulException.Unauthorized("Client authentication failed due to unknown client, no client authentication included, or unsupported authentication method.");
			// @formatter:on

			var invalidAppScope = MastodonOauthHelpers.ExpandScopes(request.Scopes ?? [])
			                                          .Except(MastodonOauthHelpers.ExpandScopes(app.Scopes))
			                                          .Any();
			if (invalidAppScope)
				throw GracefulException.BadRequest("The requested scope is invalid, unknown, or malformed.");

			app.Token ??= CryptographyHelpers.GenerateRandomString(32);
			await db.SaveChangesAsync();

			return new OauthTokenResponse
			{
				CreatedAt   = app.CreatedAt,
				Scopes      = request.Scopes ?? app.Scopes,
				AccessToken = app.Token
			};
		}

		if (request.GrantType != "authorization_code")
			throw GracefulException.BadRequest("Invalid grant_type");

		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Code == request.Code &&
		                                                          p.App.ClientId == request.ClientId &&
		                                                          p.App.ClientSecret == request.ClientSecret);
		// @formatter:off
		if (token == null)
			throw GracefulException.Unauthorized("Client authentication failed due to unknown client, no client authentication included, or unsupported authentication method.");
		if (token.Active)
			throw GracefulException.BadRequest("The provided authorization grant is invalid, expired, revoked, does not match the redirection URI used in the authorization request, or was issued to another client.");
		// @formatter:on

		var invalidScope = MastodonOauthHelpers.ExpandScopes(request.Scopes ?? [])
		                                       .Except(MastodonOauthHelpers.ExpandScopes(token.Scopes))
		                                       .Any();
		if (invalidScope)
			throw GracefulException.BadRequest("The requested scope is invalid, unknown, or malformed.");

		token.Scopes = request.Scopes ?? token.Scopes;
		token.Active = true;
		await db.SaveChangesAsync();

		return new OauthTokenResponse
		{
			CreatedAt   = token.CreatedAt,
			Scopes      = token.Scopes,
			AccessToken = token.Token
		};
	}

	[HttpPost("/oauth/revoke")]
	[ConsumesHybrid]
	[OverrideResultType<object>]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden)]
	public async Task<object> RevokeOauthToken([FromHybrid] OauthTokenRevocationRequest request)
	{
		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Token == request.Token &&
		                                                          p.App.ClientId == request.ClientId &&
		                                                          p.App.ClientSecret == request.ClientSecret) ??
		            throw GracefulException.Forbidden("You are not authorized to revoke this token");

		db.Remove(token);
		await db.SaveChangesAsync();

		return new object();
	}
}