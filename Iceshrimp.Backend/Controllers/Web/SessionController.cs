using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Shared.Schemas.Web.SessionSchemas;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for managing sessions.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[Tags("Session")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
[Route("/api/iceshrimp/sessions")]
[EnableCors("iceshrimp")]
public class SessionController(DatabaseContext db) : ControllerBase
{
	/// <summary>
	/// List Iceshrimp.NET sessions
	/// </summary>
	/// <remarks>Returns a list of Iceshrimp.NET API sessions.</remarks>
	/// <param name="page">Pagination page number</param>
	/// <response code="200">List of sessions</response>
	[HttpGet]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<SessionResponse>> GetSessions(int page = 0)
	{
		const int pageSize = 20;
		var       current  = HttpContext.GetSessionOrFail();

		return await db.Sessions
		               .Include(p => p.MastodonToken!.App)
		               .Where(p => p.User == current.User)
		               .OrderBy(p => p.Id != current.Id)
		               .ThenByDescending(p => p.LastActiveDate ?? p.CreatedAt)
		               .Skip(page * pageSize)
		               .Take(pageSize)
		               .Select(p => RenderWebSession(p, current.Id, true))
		               .ToListAsync();
	}

	/// <summary>
	/// Terminate all sessions
	/// </summary>
	/// <remarks>Terminate all Iceshrimp.NET and Mastodon sessions except the current session.</remarks>
	[HttpDelete("all")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task TerminateAllSessions()
	{
		var current = HttpContext.GetSessionOrFail();

		await db.Sessions.Where(p => p.User == current.User && p.Id != current.Id).ExecuteDeleteAsync();
		await db.OauthTokens.Where(p => p.User == current.User).ExecuteDeleteAsync();
	}

	/// <summary>
	/// Terminate Iceshrimp.NET session
	/// </summary>
	/// <remarks>Terminate the session, preventing it from accessing the Iceshrimp.NET API.</remarks>
	/// <param name="id">The session's ID</param>
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

	/// <summary>
	/// List Mastodon sessions
	/// </summary>
	/// <remarks>Returns a list of Mastodon API sessions.</remarks>
	/// <param name="page">Pagination page number</param>
	/// <response code="200">List of sessions</response>
	[HttpGet("mastodon")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<MastodonSessionResponse>> GetMastodonSessions(int page = 0)
	{
		const int pageSize = 20;
		var       current  = HttpContext.GetSessionOrFail();

		return await db.OauthTokens
		               .Include(p => p.App)
		               .Include(p => p.WebSession)
		               .Where(p => p.User == current.User)
		               .OrderByDescending(p => p.LastActiveDate ?? p.CreatedAt)
		               .Skip(page * pageSize)
		               .Take(pageSize)
		               .Select(p => RenderMastoSession(p, current.Id, true))
		               .ToListAsync();
	}

	/// <summary>
	/// Update Mastodon session
	/// </summary>
	/// <param name="id">The session's ID</param>
	/// <param name="flags">Mastodon session flags</param>
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

	/// <summary>
	/// Terminate Mastodon session
	/// </summary>
	/// <remarks>Terminate the session, preventing it from accessing the Mastodon API.</remarks>
	/// <param name="id">The session's ID</param>
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

	/// <summary>
	/// Create Mastodon session
	/// </summary>
	/// <remarks>Create a Mastodon app and session.</remarks>
	/// <param name="request">Mastodon app details</param>
	/// <response code="200">New Mastodon session</response>
	[HttpPost("mastodon")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<CreatedMastodonSessionResponse> CreateMastodonSession([FromBody] MastodonSessionRequest request)
	{
		if (HttpContext.GetSessionOrFail().MastodonTokenId != null)
			throw GracefulException.Forbidden("Refusing to create a new mastodon session from a linked web session.");
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

	private static MastodonSessionResponse RenderMastoSession(OauthToken token, string currentId, bool recurse) => new()
	{
		Id         = token.Id,
		Active     = token.Active,
		CreatedAt  = token.CreatedAt,
		LastActive = token.LastActiveDate,
		App        = token.App.Name,
		Scopes     = token.Scopes,
		Flags = new MastodonSessionFlags
		{
			SupportsHtmlFormatting = token.SupportsHtmlFormatting,
			AutoDetectQuotes       = token.AutoDetectQuotes,
			IsPleroma              = token.IsPleroma,
			SupportsInlineMedia    = token.SupportsInlineMedia
		},
		LinkedSession = recurse && token.WebSession != null
			? RenderWebSession(token.WebSession, currentId, recurse: false)
			: null
	};

	private static SessionResponse RenderWebSession(Session session, string currentId, bool recurse) => new()
	{
		Id         = session.Id,
		Current    = session.Id == currentId,
		Active     = session.Active,
		CreatedAt  = session.CreatedAt,
		LastActive = session.LastActiveDate,
		LinkedSession = recurse && session.MastodonToken != null
			? RenderMastoSession(session.MastodonToken, currentId, recurse: false)
			: null
	};
}
