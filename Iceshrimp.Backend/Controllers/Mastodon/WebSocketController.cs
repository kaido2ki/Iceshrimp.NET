using System.Net.WebSockets;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Streaming;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
public class WebSocketController(
	IHostApplicationLifetime appLifetime,
	DatabaseContext db,
	IEventService eventSvc,
	IServiceScopeFactory scopeFactory,
	ILogger<WebSocketController> logger
) : ControllerBase
{
	[Route("/api/v1/streaming")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public async Task GetStreamingSocket(
		[FromQuery(Name = "access_token")] string? accessToken,
		[FromQuery] string? stream, [FromQuery] string? list, [FromQuery] string? tag
	)
	{
		if (!HttpContext.WebSockets.IsWebSocketRequest)
			throw GracefulException.BadRequest("Not a WebSocket request");

		var ct = appLifetime.ApplicationStopping;
		accessToken ??= HttpContext.WebSockets.WebSocketRequestedProtocols.FirstOrDefault() ??
		                throw GracefulException.BadRequest("Missing WebSocket protocol header");

		var token = await Authenticate(accessToken);

		using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
		try
		{
			await WebSocketHandler.HandleConnectionAsync(webSocket, token, eventSvc, scopeFactory,
			                                             stream, list, tag, ct);
		}
		catch (Exception e)
		{
			if (e is WebSocketException)
				logger.LogDebug("WebSocket connection {id} encountered an error: {error}",
				                HttpContext.TraceIdentifier, e.Message);
			else if (!ct.IsCancellationRequested)
				throw;
		}
	}

	private async Task<OauthToken> Authenticate(string token)
	{
		return await db.OauthTokens
		               .Include(p => p.User.UserProfile)
		               .Include(p => p.User.UserSettings)
		               .Include(p => p.App)
		               .FirstOrDefaultAsync(p => p.Token == token && p.Active) ??
		       throw GracefulException.Unauthorized("This method requires an authenticated user");
	}
}