using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.HubSchemas;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using TypedSignalR.Client;

namespace Iceshrimp.Frontend.Pages;

public partial class Streaming
{
	[Inject] private SessionService? Session { get; set; }

	private readonly List<string> _messages = [];

	private HubConnection?       _hubConnection;
	private IStreamingHubServer? _hub;
	private string               _userInput    = "";
	private string               _messageInput = "";

	private class StreamingHubClient(Streaming page) : IStreamingHubClient, IHubConnectionObserver
	{
		public Task ReceiveMessage(string user, string message)
		{
			var encodedMsg = $"{user}: {message}";
			page._messages.Add(encodedMsg);
			page.InvokeAsync(page.StateHasChanged);
			return Task.CompletedTask;
		}

		public Task Notification(NotificationResponse notification)
		{
			var encodedMsg = $"Notification: {notification.Id} ({notification.Type})";
			page._messages.Add(encodedMsg);
			page.InvokeAsync(page.StateHasChanged);
			return Task.CompletedTask;
		}

		public Task NotePublished(List<StreamingTimeline> timelines, NoteResponse note)
		{
			var encodedMsg = $"Note: {note.Id}";
			page._messages.Add(encodedMsg);
			page.InvokeAsync(page.StateHasChanged);
			return Task.CompletedTask;
		}

		public Task NoteUpdated(List<StreamingTimeline> timelines, NoteResponse note)
		{
			var encodedMsg = $"Note update: {note.Id}";
			page._messages.Add(encodedMsg);
			page.InvokeAsync(page.StateHasChanged);
			return Task.CompletedTask;
		}

		public Task OnClosed(Exception? exception)
		{
			return ReceiveMessage("System", "Connection closed.");
		}

		public Task OnReconnected(string? connectionId)
		{
			return ReceiveMessage("System", "Reconnected.");
		}

		public Task OnReconnecting(Exception? exception)
		{
			return ReceiveMessage("System", "Reconnecting...");
		}
	}

	protected override async Task OnInitializedAsync()
	{
		_hubConnection = new HubConnectionBuilder()
		                 .WithUrl(Navigation.ToAbsoluteUri("/hubs/streaming"), Auth)
		                 .AddMessagePackProtocol()
		                 .Build();

		// This must be in a .razor.cs file for the code generator to work correctly
		_hub = _hubConnection.CreateHubProxy<IStreamingHubServer>();

		_hubConnection.Register<IStreamingHubClient>(new StreamingHubClient(this));

		try
		{
			await _hubConnection.StartAsync();
			await _hub.Subscribe(StreamingTimeline.Home);
		}
		catch (Exception e)
		{
			_messages.Add($"System: Connection failed - {e.Message}");
			await InvokeAsync(StateHasChanged);
		}

		return;

		void Auth(HttpConnectionOptions options)
		{
			options.AccessTokenProvider = () => Task.FromResult(Session?.Current?.Token);
		}
	}

	private async Task Send()
	{
		if (_hub is not null)
		{
			await _hub.SendMessage(_userInput, _messageInput);
		}
	}

	private bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection is not null)
		{
			await _hubConnection.DisposeAsync();
		}
	}
}