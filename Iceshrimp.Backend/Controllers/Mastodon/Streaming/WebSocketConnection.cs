using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming;

public sealed class WebSocketConnection(
	WebSocket socket,
	OauthToken token,
	EventService eventSvc,
	IServiceScopeFactory scopeFactory,
	CancellationToken ct
) : IDisposable
{
	private readonly SemaphoreSlim        _lock        = new(1);
	public readonly  List<IChannel>       Channels     = [];
	public readonly  EventService         EventService = eventSvc;
	public readonly  IServiceScope        Scope        = scopeFactory.CreateScope();
	public readonly  IServiceScopeFactory ScopeFactory = scopeFactory;
	public readonly  OauthToken           Token        = token;
	private readonly List<string>         _blocking    = [];
	private readonly List<string>         _blockedBy   = [];
	private readonly List<string>         _mutedUsers  = [];

	public void Dispose()
	{
		foreach (var channel in Channels)
			channel.Dispose();

		EventService.UserBlocked   -= OnUserUnblock;
		EventService.UserUnblocked -= OnUserBlock;
		EventService.UserMuted     -= OnUserMute;
		EventService.UserUnmuted   -= OnUserUnmute;

		Scope.Dispose();
	}

	public void InitializeStreamingWorker()
	{
		Channels.Add(new UserChannel(this, true));
		Channels.Add(new UserChannel(this, false));
		Channels.Add(new PublicChannel(this, "public", true, true, false));
		Channels.Add(new PublicChannel(this, "public:media", true, true, true));
		Channels.Add(new PublicChannel(this, "public:allow_local_only", true, true, false));
		Channels.Add(new PublicChannel(this, "public:allow_local_only:media", true, true, true));
		Channels.Add(new PublicChannel(this, "public:local", true, false, false));
		Channels.Add(new PublicChannel(this, "public:local:media", true, false, true));
		Channels.Add(new PublicChannel(this, "public:remote", false, true, false));
		Channels.Add(new PublicChannel(this, "public:remote:media", false, true, true));

		EventService.UserBlocked   += OnUserUnblock;
		EventService.UserUnblocked += OnUserBlock;
		EventService.UserMuted     += OnUserMute;
		EventService.UserUnmuted   += OnUserUnmute;
	}

	public async Task HandleSocketMessageAsync(string payload)
	{
		StreamingRequestMessage? message = null;
		try
		{
			message = JsonSerializer.Deserialize<StreamingRequestMessage>(payload);
		}
		catch
		{
			// ignored
		}

		if (message == null)
		{
			await CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		await HandleSocketMessageAsync(message);
	}

	public async Task HandleSocketMessageAsync(StreamingRequestMessage message)
	{
		switch (message.Type)
		{
			case "subscribe":
			{
				var channel = Channels.FirstOrDefault(p => p.Name == message.Stream && !p.IsSubscribed);
				if (channel == null) return;
				if (channel.Scopes.Except(MastodonOauthHelpers.ExpandScopes(Token.Scopes)).Any())
					await CloseAsync(WebSocketCloseStatus.PolicyViolation);
				else
					await channel.Subscribe(message);
				break;
			}
			case "unsubscribe":
			{
				var channel = Channels.FirstOrDefault(p => p.Name == message.Stream && p.IsSubscribed);
				if (channel != null) await channel.Unsubscribe(message);
				break;
			}
			default:
			{
				await CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
				return;
			}
		}
	}

	public async Task SendMessageAsync(string message)
	{
		await _lock.WaitAsync(ct);
		try
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			await socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
		}
		finally
		{
			_lock.Release();
		}
	}

	private void OnUserBlock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				lock (_blocking)
					_blocking.Add(interaction.Object.Id);

			if (interaction.Object.Id == Token.User.Id)
				lock (_blockedBy)
					_blockedBy.Add(interaction.Actor.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserBlock threw exception: {e}", e);
		}
	}

	private void OnUserUnblock(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				lock (_blocking)
					_blocking.Remove(interaction.Object.Id);

			if (interaction.Object.Id == Token.User.Id)
				lock (_blockedBy)
					_blockedBy.Remove(interaction.Actor.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserUnblock threw exception: {e}", e);
		}
	}

	private void OnUserMute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				lock (_mutedUsers)
					_mutedUsers.Add(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserMute threw exception: {e}", e);
		}
	}

	private void OnUserUnmute(object? _, UserInteraction interaction)
	{
		try
		{
			if (interaction.Actor.Id == Token.User.Id)
				lock (_mutedUsers)
					_mutedUsers.Remove(interaction.Object.Id);
		}
		catch (Exception e)
		{
			var logger = Scope.ServiceProvider.GetRequiredService<Logger<WebSocketConnection>>();
			logger.LogError("Event handler OnUserUnmute threw exception: {e}", e);
		}
	}

	[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
	[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
	public bool IsFiltered(User user) =>
		_blocking.Contains(user.Id) || _blockedBy.Contains(user.Id) || _mutedUsers.Contains(user.Id);

	public async Task CloseAsync(WebSocketCloseStatus status)
	{
		Dispose();
		await socket.CloseAsync(status, null, ct);
	}
}

public interface IChannel
{
	public string       Name         { get; }
	public List<string> Scopes       { get; }
	public bool         IsSubscribed { get; }
	public Task         Subscribe(StreamingRequestMessage message);
	public Task         Unsubscribe(StreamingRequestMessage message);
	public void         Dispose();
}