using Iceshrimp.Frontend.Core.Schemas;
using Iceshrimp.Shared.HubSchemas;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using TypedSignalR.Client;

namespace Iceshrimp.Frontend.Core.Services;

using NoteEvent = (StreamingTimeline timeline, NoteResponse note);

internal class StreamingService(
	SessionService session,
	NavigationManager navigation,
	ILogger<StreamingService> logger
) : IAsyncDisposable
{
	private HubConnection?       _hubConnection;
	private IStreamingHubServer? _hub;

	public event EventHandler<string>?               Message;
	public event EventHandler<NotificationResponse>? Notification;
	public event EventHandler<NoteEvent>?            NotePublished;
	public event EventHandler<NoteEvent>?            NoteUpdated;
	public event EventHandler?                       OnConnectionChange;

	public async Task Connect(StoredUser? user = null)
	{
		if (_hubConnection != null)
		{
			await _hubConnection.DisposeAsync();
			_hubConnection = null;
		}

		user ??= session.Current;

		if (user == null)
			return;

		_hubConnection = new HubConnectionBuilder()
		                 .WithUrl(navigation.ToAbsoluteUri("/hubs/streaming"), Auth)
		                 .AddMessagePackProtocol()
		                 .Build();

		_hub = _hubConnection.CreateHubProxy<IStreamingHubServer>();
		_hubConnection.Register<IStreamingHubClient>(new StreamingHubClient(this));

		try
		{
			await _hubConnection.StartAsync();
			await _hub.Subscribe(StreamingTimeline.Home);
		}
		catch (Exception e)
		{
			Message?.Invoke(this, $"System: Connection failed - {e.Message}");
			logger.LogError("Connection failed: {error}", e.Message);
		}

		return;

		void Auth(HttpConnectionOptions options)
		{
			options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
		}
	}

	public async Task Send(string userInput, string messageInput)
	{
		if (_hub is not null)
			await _hub.SendMessage(userInput, messageInput);
	}

	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection is not null)
		{
			await _hubConnection.DisposeAsync();
		}
	}

	private class StreamingHubClient(StreamingService streaming) : IStreamingHubClient, IHubConnectionObserver
	{
		public Task ReceiveMessage(string user, string message)
		{
			var encodedMsg = $"{user}: {message}";
			streaming.Message?.Invoke(this, encodedMsg);
			return Task.CompletedTask;
		}

		public Task Notification(NotificationResponse notification)
		{
			streaming.Notification?.Invoke(this, notification);
			return Task.CompletedTask;
		}

		public Task NotePublished(List<StreamingTimeline> timelines, NoteResponse note)
		{
			foreach (var timeline in timelines)
				streaming.NotePublished?.Invoke(this, (timeline, note));
			return Task.CompletedTask;
		}

		public Task NoteUpdated(List<StreamingTimeline> timelines, NoteResponse note)
		{
			foreach (var timeline in timelines)
				streaming.NoteUpdated?.Invoke(this, (timeline, note));
			return Task.CompletedTask;
		}

		public Task OnClosed(Exception? exception)
		{
			streaming.OnConnectionChange?.Invoke(this, EventArgs.Empty);
			return ReceiveMessage("System", "Connection closed.");
		}

		public Task OnReconnected(string? connectionId)
		{
			streaming.OnConnectionChange?.Invoke(this, EventArgs.Empty);
			return ReceiveMessage("System", "Reconnected.");
		}

		public Task OnReconnecting(Exception? exception)
		{
			return ReceiveMessage("System", "Reconnecting...");
		}
	}
}