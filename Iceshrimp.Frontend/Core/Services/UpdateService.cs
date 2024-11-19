using Iceshrimp.Frontend.Components;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Iceshrimp.Frontend.Core.Services;

internal class UpdateService
{
	private readonly ApiService                     _api;
	private readonly ILogger<UpdateService>         _logger;
	private readonly GlobalComponentSvc             _globalComponentSvc;
	private readonly Lazy<Task<IJSObjectReference>> _moduleTask;
	private readonly NavigationManager              _nav;

	private VersionInfo      FrontendVersion { get; } = VersionHelpers.VersionInfo.Value;
	public  VersionResponse? BackendVersion  { get; private set; }

	// ReSharper disable once UnusedAutoPropertyAccessor.Local
	private Timer Timer { get; set; }

	public UpdateService(
		ApiService api, ILogger<UpdateService> logger, GlobalComponentSvc globalComponentSvc, IJSRuntime js,
		NavigationManager nav
	)
	{
		_api                = api;
		_logger             = logger;
		_globalComponentSvc = globalComponentSvc;
		_nav                = nav;

		_moduleTask = new Lazy<Task<IJSObjectReference>>(() => js.InvokeAsync<IJSObjectReference>(
		                                                          "import",
		                                                          "./Core/Services/UpdateService.cs.js")
		                                                         .AsTask());
		Timer = new Timer(CallbackAsync, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
		_     = RegisterUpdateCallbackAsync();
	}

	private async Task RegisterUpdateCallbackAsync()
	{
		var module = await _moduleTask.Value;
		var objRef = DotNetObjectReference.Create(this);
		await module.InvokeAsync<string>("RegisterUpdateCallback", objRef);
	}

	[JSInvokable]
	public void OnUpdateFound()
	{
		var banner = new BannerContainer.Banner
		{
			Text = "New version available", OnTap = () => { _nav.NavigateTo("/settings/about"); }
		};
		_globalComponentSvc.BannerComponent?.AddBanner(banner);
	}

	public async Task<bool> ServiceWorkerCheckWaitingAsync()
	{
		var module = await _moduleTask.Value;
		return await module.InvokeAsync<bool>("ServiceWorkerCheckWaiting");
	}

	public async Task ServiceWorkerUpdateAsync()
	{
		var module = await _moduleTask.Value;
		await module.InvokeVoidAsync("ServiceWorkerUpdate");
	}

	public async Task<bool> ServiceWorkerSkipWaitingAsync()
	{
		var module = await _moduleTask.Value;
		return await module.InvokeAsync<bool>("ServiceWorkerSkipWaiting");
	}

	private async void CallbackAsync(object? _)
	{
		await CheckVersionAsync();
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
			await ServiceWorkerUpdateAsync();
		}
	}
}