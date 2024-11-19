using Iceshrimp.Frontend.Core.Schemas;
using Iceshrimp.Shared.Schemas.SignalR;
using Iceshrimp.Shared.Schemas.Web;
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
	private IStreamingHubServer? _hub;
	private HubConnection?       _hubConnection;

	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection is not null)
		{
			await _hubConnection.DisposeAsync();
		}
	}

	public event EventHandler<NotificationResponse>? Notification;
	public event EventHandler<NoteEvent>?            NotePublished;
	public event EventHandler<NoteResponse>?         NoteUpdated;
	public event EventHandler<string>?               NoteDeleted;
	public event EventHandler<FilterResponse>?       FilterAdded;
	public event EventHandler<FilterResponse>?       FilterUpdated;
	public event EventHandler<long>?                 FilterRemoved;
	public event EventHandler<HubConnectionState>?   OnConnectionChange;

	public async Task ConnectAsync(StoredUser? user = null)
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
		                 .WithAutomaticReconnect()
		                 .WithStatefulReconnect()
		                 .AddMessagePackProtocol()
		                 .Build();

		_hub = _hubConnection.CreateHubProxy<IStreamingHubServer>();
		_hubConnection.Register<IStreamingHubClient>(new StreamingHubClient(this));

		try
		{
			await _hubConnection.StartAsync();
			await _hub.SubscribeAsync(StreamingTimeline.Home);
		}
		catch (Exception e)
		{
			OnConnectionChange?.Invoke(this, HubConnectionState.Disconnected);
			logger.LogError("Connection failed: {error}", e.Message);
		}

		return;

		void Auth(HttpConnectionOptions options) =>
			options.AccessTokenProvider = () => Task.FromResult<string?>(user.Token);
	}

	public async Task ReconnectAsync(StoredUser? user = null)
	{
		if (_hubConnection is null)
		{
			await ConnectAsync(user);
			return;
		}

		if (_hubConnection.State is not HubConnectionState.Disconnected) return;
		await _hubConnection.StartAsync();
	}

	private class StreamingHubClient(StreamingService streaming) : IStreamingHubClient, IHubConnectionObserver
	{
		public Task OnClosed(Exception? exception)
		{
			streaming.OnConnectionChange?.Invoke(this, HubConnectionState.Disconnected);
			return Task.CompletedTask;
		}

		public Task OnReconnected(string? connectionId)
		{
			streaming.OnConnectionChange?.Invoke(this, HubConnectionState.Connected);
			return Task.CompletedTask;
		}

		public Task OnReconnecting(Exception? exception)
		{
			streaming.OnConnectionChange?.Invoke(this, HubConnectionState.Reconnecting);
			return Task.CompletedTask;
		}

		public Task NotificationAsync(NotificationResponse notification)
		{
			streaming.Notification?.Invoke(this, notification);
			return Task.CompletedTask;
		}

		public Task NotePublishedAsync(List<StreamingTimeline> timelines, NoteResponse note)
		{
			foreach (var timeline in timelines)
				streaming.NotePublished?.Invoke(this, (timeline, note));
			return Task.CompletedTask;
		}

		public Task NoteUpdatedAsync(NoteResponse note)
		{
			streaming.NoteUpdated?.Invoke(this, note);
			return Task.CompletedTask;
		}

		public Task NoteDeletedAsync(string noteId)
		{
			streaming.NoteDeleted?.Invoke(this, noteId);
			return Task.CompletedTask;
		}

		public Task FilterAddedAsync(FilterResponse filter)
		{
			streaming.FilterAdded?.Invoke(this, filter);
			return Task.CompletedTask;
		}

		public Task FilterUpdatedAsync(FilterResponse filter)
		{
			streaming.FilterUpdated?.Invoke(this, filter);
			return Task.CompletedTask;
		}

		public Task FilterRemovedAsync(long filterId)
		{
			streaming.FilterRemoved?.Invoke(this, filterId);
			return Task.CompletedTask;
		}
	}
}