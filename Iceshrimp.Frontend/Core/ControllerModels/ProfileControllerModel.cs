using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class ProfileControllerModel(ApiClient api)
{
	public Task<UserProfileEntity> GetProfileAsync() =>
		api.CallAsync<UserProfileEntity>(HttpMethod.Get, "/profile");

	public Task UpdateProfileAsync(UserProfileEntity request) =>
		api.CallAsync(HttpMethod.Put, "/profile", data: request);
}