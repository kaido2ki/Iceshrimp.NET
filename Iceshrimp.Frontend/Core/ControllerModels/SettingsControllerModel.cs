using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SettingsControllerModel(ApiClient api)
{
	public Task<UserSettingsEntity> GetSettings() => api.Call<UserSettingsEntity>(HttpMethod.Get, "/settings");

	public Task<bool> UpdateSettings(UserSettingsEntity settings) =>
		api.CallNullable(HttpMethod.Put, "/settings", data: settings);
}