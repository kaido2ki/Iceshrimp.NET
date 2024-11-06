using System.Timers;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Timer = System.Timers.Timer;

namespace Iceshrimp.Frontend.Core.Services;

internal class UpdateService
{
	private readonly ApiService             _api;
	private readonly ILogger<UpdateService> _logger;

	public UpdateService(ApiService api, ILogger<UpdateService> logger)
	{
		_api                =  api;
		_logger             =  logger;
		UpdateTimer         =  new Timer { AutoReset = true, Enabled = true, Interval = 60000, };
		UpdateTimer.Elapsed += (_, _) => CheckVersion();
		CheckVersion();
	}

	private VersionInfo FrontendVersion { get; } = VersionHelpers.GetVersionInfo();
	private Timer       UpdateTimer     { get; }

	private async Task<VersionResponse?> GetVersion()
	{
		try
		{
			var backendVersion = await _api.Version.GetVersion();
			_logger.LogInformation("Successfully fetched backend version.");
			return backendVersion;
		}
		catch (ApiException e)
		{
			_logger.LogError(e, "Failed to fetch backend version.");
			return null;
		}
	}

	private async void CheckVersion()
	{
		var version = await GetVersion();
		if (version is null) return;
		if (version.Version != FrontendVersion.Version)
		{
			throw new NotImplementedException();
		}
	}
}