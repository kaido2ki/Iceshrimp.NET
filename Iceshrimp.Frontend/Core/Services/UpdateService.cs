using System.Diagnostics;
using Iceshrimp.Shared.Helpers;
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

	private VersionInfo      FrontendVersion { get; } = VersionHelpers.VersionInfo.Value;
	public  VersionResponse? BackendVersion  { get; private set; }

	// ReSharper disable once UnusedAutoPropertyAccessor.Local
	private Timer  CheckUpdateTimer  { get; set; }
	private Timer? CheckWaitingTimer { get; set; }

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
		CheckUpdateTimer = new Timer(UpdateCheckCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
		_                = RegisterSwUpdateCallbackAsync();
	}

	private async Task RegisterSwUpdateCallbackAsync()
	{
		var module = await _moduleTask.Value;
		var objRef = DotNetObjectReference.Create(this);
		await module.InvokeAsync<string>("RegisterSWUpdateCallback", objRef);
	}

	[JSInvokable]
	public void NewServiceWorker()
	{
		UpdateState       = UpdateStates.UpdateInstalling;
		CheckWaitingTimer = new Timer(CheckWaitingCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
	}

	private async Task ServiceWorkerCheckWaitingAsync()
	{
		var res = await ServiceWorkerCheckStateAsync();
		_logger.LogInformation("Checking for service worker state.");
		if (res == ServiceWorkerState.Waiting)
		{
			_logger.LogInformation("New service worker found, stopping checks.");
			CheckWaitingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			CheckWaitingTimer?.Dispose();
			UpdateState = UpdateStates.UpdateInstalled;
		}
	}

	private void UpdateCheckCallback(object? caller)
	{
		_ = CheckVersionAsync();
	}

	private void CheckWaitingCallback(object? caller)
	{
		_ = ServiceWorkerCheckWaitingAsync();
	}

	public async Task ServiceWorkerUnregisterAllAsync()
	{
		var module = await _moduleTask.Value;
		var res = await module.InvokeAsync<bool>("UnregisterAll");
		if (!res) _logger.LogError("Failed to unregister all service workers");
	}

	public async Task ServiceWorkerUpdateAsync()
	{
		var module = await _moduleTask.Value;
		var res    = await module.InvokeAsync<string?>("ServiceWorkerUpdate");
		if (res is null) return;
		UpdateState = res switch
		{
			"installing" => UpdateStates.UpdateInstalling,
			"waiting"    => UpdateStates.UpdateInstalled,
			"active"     => UpdateStates.NoUpdate,
			null         => UpdateStates.Error,
			_            => throw new UnreachableException()
		};
		if (UpdateState == UpdateStates.UpdateInstalling)
		{
			CheckWaitingTimer = new Timer(CheckWaitingCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
		}
	}

	private async Task<ServiceWorkerState> ServiceWorkerCheckStateAsync()
	{
		var module = await _moduleTask.Value;
		var res    = await module.InvokeAsync<string>("ServiceWorkerCheckRegistration");
		_logger.LogTrace($"aService worker state: {res}");
		return res switch
		{
			"installing" => ServiceWorkerState.Installing,
			"waiting"    => ServiceWorkerState.Waiting,
			"active"     => ServiceWorkerState.Active,
			null         => ServiceWorkerState.Error,
			_            => throw new UnreachableException()
		};
	}

	public async Task<bool> ServiceWorkerSkipWaitingAsync()
	{
		var module = await _moduleTask.Value;
		var res    = await module.InvokeAsync<bool?>("ServiceWorkerSkipWaiting");
		if (res is null) throw new Exception("Error occured while updating service worker.");
		return (bool)res;
	}

	private async Task<VersionResponse?> GetVersionAsync()
	{
		try
		{
			var backendVersion = await _api.Version.GetVersionAsync();
			_logger.LogInformation("Successfully fetched backend version.");
			return backendVersion;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to fetch backend version.");
			return null;
		}
	}

	private async Task CheckVersionAsync()
	{
		var version = await GetVersionAsync();
		if (version is null) return;
		BackendVersion = version;
		if (version.Version != FrontendVersion.Version)
		{
			UpdateState = UpdateStates.UpdateAvailable;
			await ServiceWorkerUpdateAsync();
		}
	}

	internal enum UpdateStates
	{
		NoUpdate,
		UpdateAvailable,
		UpdateInstalling,
		UpdateInstalled,
		Error
	}

	private enum ServiceWorkerState
	{
		Installing,
		Waiting,
		Active,
		Error
	}
}
