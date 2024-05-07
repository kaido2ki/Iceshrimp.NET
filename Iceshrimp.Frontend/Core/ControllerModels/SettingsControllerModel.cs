using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SettingsControllerModel(ApiClient api)
{
	public Task<UserSettingsEntity> GetSettings() => api.Call<UserSettingsEntity>(HttpMethod.Get, "/settings");
	public Task UpdateSettings(UserSettingsEntity settings) => api.Call(HttpMethod.Put, "/settings", data: settings);
}