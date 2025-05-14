using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Shared.Schemas.Web.SessionSchemas;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[Tags("Session")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[Route("/api/iceshrimp/sessions")]
public class SessionController(DatabaseContext db) : ControllerBase
{
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<SessionResponse>> GetSessions(int page = 0)
	{
		const int pageSize  = 20;
		var       currentId = HttpContext.GetSessionOrFail().Id;

		return await db.Sessions
		               .Where(p => p.User == HttpContext.GetUserOrFail())
		               .OrderByDescending(p => p.LastActiveDate ?? p.CreatedAt)
		               .Skip(page * pageSize)
		               .Take(pageSize)
		               .Select(p => new SessionResponse
		               {
			               Id         = p.Id,
			               Current    = p.Id == currentId,
			               Active     = p.Active,
			               CreatedAt  = p.CreatedAt,
			               LastActive = p.LastActiveDate
		               })
		               .ToListAsync();
	}

	[HttpDelete("{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task TerminateSession(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var session = await db.Sessions.Include(p => p.MastodonToken)
		                      .FirstOrDefaultAsync(p => p.Id == id && p.User == user)
		              ?? throw GracefulException.NotFound("Session not found");

		if (session.Id == HttpContext.GetSessionOrFail().Id)
			throw GracefulException.BadRequest("Refusing to terminate current session");

		if (session.MastodonToken != null)
			db.Remove(session.MastodonToken);
		db.Remove(session);

		await db.SaveChangesAsync();
	}

	[HttpGet("mastodon")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<MastodonSessionResponse>> GetMastodonSessions(int page = 0)
	{
		const int pageSize = 20;

		return await db.OauthTokens
		               .Include(p => p.App)
		               .Where(p => p.User == HttpContext.GetUserOrFail())
		               .OrderByDescending(p => p.LastActiveDate ?? p.CreatedAt)
		               .Skip(page * pageSize)
		               .Take(pageSize)
		               .Select(p => new MastodonSessionResponse
		               {
			               Id         = p.Id,
			               Active     = p.Active,
			               CreatedAt  = p.CreatedAt,
			               LastActive = p.LastActiveDate,
			               App        = p.App.Name,
			               Scopes     = p.Scopes,
			               Flags = new MastodonSessionFlags
			               {
				               SupportsHtmlFormatting = p.SupportsHtmlFormatting,
				               AutoDetectQuotes       = p.AutoDetectQuotes,
				               IsPleroma              = p.IsPleroma,
				               SupportsInlineMedia    = p.SupportsInlineMedia
			               }
		               })
		               .ToListAsync();
	}

	[HttpPatch("mastodon/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task UpdateMastodonSession(string id, [FromBody] MastodonSessionFlags flags)
	{
		var user = HttpContext.GetUserOrFail();
		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Id == id && p.User == user)
		            ?? throw GracefulException.NotFound("Session not found");

		token.SupportsHtmlFormatting = flags.SupportsHtmlFormatting;
		token.AutoDetectQuotes       = flags.AutoDetectQuotes;
		token.IsPleroma              = flags.IsPleroma;
		token.SupportsInlineMedia    = flags.SupportsInlineMedia;

		await db.SaveChangesAsync();
	}

	[HttpDelete("mastodon/{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
	public async Task TerminateMastodonSession(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var token = await db.OauthTokens.FirstOrDefaultAsync(p => p.Id == id && p.User == user)
		            ?? throw GracefulException.NotFound("Session not found");

		db.Remove(token);
		await db.SaveChangesAsync();
	}

	[HttpPost("mastodon")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<MastodonSessionResponse> CreateMastodonSession([FromBody] MastodonSessionRequest request)
	{
		if (!MastodonOauthHelpers.ValidateScopes(request.Scopes))
			throw GracefulException.BadRequest("Invalid scopes parameter");

		var user = HttpContext.GetUserOrFail();

		var app = new OauthApp
		{
			Id           = IdHelpers.GenerateSnowflakeId(),
			ClientId     = CryptographyHelpers.GenerateRandomString(32),
			ClientSecret = CryptographyHelpers.GenerateRandomString(32),
			CreatedAt    = DateTime.UtcNow,
			Name         = request.AppName,
			Website      = null,
			Scopes       = request.Scopes,
			RedirectUris = ["urn:ietf:wg:oauth:2.0:oob"]
		};

		var token = new OauthToken
		{
			Id                     = IdHelpers.GenerateSnowflakeId(),
			Active                 = true,
			Code                   = CryptographyHelpers.GenerateRandomString(32),
			Token                  = CryptographyHelpers.GenerateRandomString(32),
			App                    = app,
			User                   = user,
			CreatedAt              = DateTime.UtcNow,
			Scopes                 = request.Scopes,
			RedirectUri            = "urn:ietf:wg:oauth:2.0:oob",
			AutoDetectQuotes       = request.Flags.AutoDetectQuotes,
			SupportsHtmlFormatting = request.Flags.SupportsHtmlFormatting,
			IsPleroma              = request.Flags.IsPleroma,
			SupportsInlineMedia    = request.Flags.SupportsInlineMedia,
		};

		db.Add(token);
		await db.SaveChangesAsync();
		await db.Entry(token).ReloadAsync();

		return new CreatedMastodonSessionResponse
		{
			Id         = token.Id,
			Active     = token.Active,
			CreatedAt  = token.CreatedAt,
			LastActive = token.LastActiveDate,
			App        = token.App.Name,
			Scopes     = token.Scopes,
			Flags      = request.Flags,
			Token      = token.Token
		};
	}
}
