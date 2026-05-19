using Iceshrimp.Shared.Schemas.Web;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

internal class UpdateService
{
	private readonly ApiService                     _api;
	private readonly ILogger<UpdateService>         _logger;
	private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
	public           bool                           DialogOpen { get; set; }

	public EventHandler<UpdateStates>? UpdateStatusEvent { get; set; }

	public UpdateStates UpdateState
	{
		get;
		private set
		{
			UpdateStatusEvent?.Invoke(this, value);
			_logger.LogInformation($"Invoked Update Status Event: {value}");
			field = value;
		}
	}

	public  VersionResponse? BackendVersion  { get; private set; }

	public UpdateService(
		ApiService api, ILogger<UpdateService> logger, IJSRuntime js
	)
	{
		_api    = api;
		_logger = logger;

		_moduleTask = new Lazy<Task<IJSObjectReference>>(() => js.InvokeAsync<IJSObjectReference>(
			                                                          "import",
			                                                          "./Core/Services/UpdateService.cs.js")
		                                                         .AsTask());
		_ = StartSwUpdateCheckingAsync();
	}

	private async Task StartSwUpdateCheckingAsync()
	{
		var module = await _moduleTask.Value;
		var objRef = DotNetObjectReference.Create(this);
		await module.InvokeVoidAsync("startSwUpdateChecking", objRef);

		BackendVersion = await _api.Version.GetVersionAsync();
	}

	[JSInvokable]
	public void UpdateReady()
	{
		UpdateState = UpdateStates.UpdateInstalled;
		Console.WriteLine("Update available");
	}

	[JSInvokable]
	public void NoUpdate()
	{
		UpdateState = UpdateStates.NoUpdate;
		Console.WriteLine("No new updates");
	}

	public async Task SwSkipWaitingAsync()
	{
		var module = await _moduleTask.Value;
		await module.InvokeVoidAsync("swSkipWaiting");
	}

	public async Task UnregisterAllSwAsync()
	{
		var module = await _moduleTask.Value;
		await module.InvokeVoidAsync("unregisterAllSw");
	}

	internal enum UpdateStates
	{
		NoUpdate,
		UpdateInstalled,
		Error
	}
}
