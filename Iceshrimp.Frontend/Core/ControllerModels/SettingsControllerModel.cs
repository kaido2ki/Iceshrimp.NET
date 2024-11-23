using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SettingsControllerModel(ApiClient api)
{
	public Task<UserSettingsResponse> GetSettingsAsync() =>
		api.CallAsync<UserSettingsResponse>(HttpMethod.Get, "/settings");

	public Task<bool> UpdateSettingsAsync(UserSettingsRequest settings) =>
		api.CallNullableAsync(HttpMethod.Put, "/settings", data: settings);
}