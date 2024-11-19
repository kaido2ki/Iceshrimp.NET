using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SettingsControllerModel(ApiClient api)
{
	public Task<UserSettingsEntity> GetSettingsAsync() => api.CallAsync<UserSettingsEntity>(HttpMethod.Get, "/settings");

	public Task<bool> UpdateSettingsAsync(UserSettingsEntity settings) =>
		api.CallNullableAsync(HttpMethod.Put, "/settings", data: settings);
}