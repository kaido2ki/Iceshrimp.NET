using Iceshrimp.Shared.HubSchemas;
using Microsoft.AspNetCore.SignalR.Client;
using TypedSignalR.Client;

namespace Iceshrimp.Frontend.Pages;

public partial class Hub
{
	private          IExampleHubServer? _hub;
	private          HubConnection?     _hubConnection;
	private          string             _messageInput = "";
	private readonly List<string>       _messages     = [];
	private          string             _userInput    = "";

	private bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection is not null)
		{
			await _hubConnection.DisposeAsync();
		}
	}

	protected override async Task OnInitializedAsync()
	{
		_hubConnection = new HubConnectionBuilder()
		                 .WithUrl(Navigation.ToAbsoluteUri("/hubs/example"))
		                 .AddMessagePackProtocol()
		                 .Build();

		// This must be in a .razor.cs file for the code generator to work correctly
		_hub = _hubConnection.CreateHubProxy<IExampleHubServer>();

		//TODO: authentication is done like this:
		//options => { options.AccessTokenProvider = () => Task.FromResult("the_access_token")!; })

		_hubConnection.Register<IExampleHubClient>(new ExampleHubClient(this));

		await _hubConnection.StartAsync();
	}

	private async Task Send()
	{
		if (_hub is not null)
		{
			await _hub.SendMessage(_userInput, _messageInput);
		}
	}

	private class ExampleHubClient(Hub page) : IExampleHubClient, IHubConnectionObserver
	{
		public Task ReceiveMessage(string user, string message)
		{
			var encodedMsg = $"{user}: {message}";
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
}