using Blazored.LocalStorage;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Schemas;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services;

internal class SettingsService(ApiService api, ISyncLocalStorageService localStorage, ILogger<SettingsService> logger)
{
	private UserSettingsResponse? UserSettings { get; set; }

	/// <summary>
	/// Preferences for the frontend, stored in local storage. Preferences are saved based on the setter, so you cannot directly set Preferences.CustomCss = "";
	/// </summary>
	public ClientPreferences Preferences
	{
		get => localStorage.GetItem<ClientPreferences>("preferences") ?? new ClientPreferences();
		set => localStorage.SetItem("preferences", value);
	}

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
