using Blazored.LocalStorage;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Schemas;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services;

internal class SettingsService(ApiService api, ISyncLocalStorageService localStorage, ILogger<SettingsService> logger)
{
	private UserSettingsResponse? UserSettings { get; set; }

	/// <summary>
	/// Preferences for the frontend, stored in local storage. Save client preferences with <see cref="SaveClientPreferences"/>.
	/// </summary>
	public ClientPreferences Preferences { get; private set; } =
		localStorage.GetItem<ClientPreferences>("preferences") ?? new ClientPreferences();

	public void SaveClientPreferences(ClientPreferences preferences)
	{
		Preferences = preferences;
		localStorage.SetItem("preferences", Preferences);
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
