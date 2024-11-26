using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services;

internal class SettingsService(ApiService api, ILogger<SettingsService> logger)
{
	private UserSettingsResponse? UserSettings { get; set; }

	public async Task<UserSettingsResponse> GetUserSettingsAsync()
	{
		if (UserSettings is null)
		{
			while (UserSettings is null)
			{
				await UpdateUserSettingsAsync();
			}
		}

		_ = UpdateUserSettingsAsync();
		return UserSettings;
	}

	private async Task UpdateUserSettingsAsync()
	{
		try
		{
			UserSettings = await api.Settings.GetSettingsAsync();
		}
		catch (ApiException e)
		{
			logger.LogError(e, "Failed to fetch settings");
		}
	}
}
