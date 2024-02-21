using System.Net.WebSockets;
using System.Text;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming;

public static class WebSocketHandler
{
	public static async Task HandleConnectionAsync(
		WebSocket socket, OauthToken token, EventService eventSvc, IServiceScopeFactory scopeFactory,
		CancellationToken ct
	)
	{
		using var connection = new WebSocketConnection(socket, token, eventSvc, scopeFactory, ct);
		var       buffer     = new byte[256];

		WebSocketReceiveResult? res = null;

		connection.InitializeStreamingWorker();

		while ((!res?.CloseStatus.HasValue ?? true) &&
		       !ct.IsCancellationRequested &&
		       socket.State is WebSocketState.Open)
		{
			res = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

			if (res.Count > buffer.Length)
			{
				await socket.CloseAsync(WebSocketCloseStatus.MessageTooBig, null, ct);
				break;
			}

			if (res.MessageType == WebSocketMessageType.Text)
				await connection.HandleSocketMessageAsync(Encoding.UTF8.GetString(buffer, 0, res.Count));
			else if (res.MessageType == WebSocketMessageType.Binary)
				break;
		}

		if (socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
			return;

		if (res?.CloseStatus != null)
			await socket.CloseAsync(res.CloseStatus.Value, res.CloseStatusDescription, ct);
		else if (!ct.IsCancellationRequested)
			await socket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, null, ct);
		else
			await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, null, ct);
	}
}