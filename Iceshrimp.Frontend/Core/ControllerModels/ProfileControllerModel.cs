using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class ProfileControllerModel(ApiClient api)
{
	public Task<UserProfileEntity> GetProfile() =>
		api.Call<UserProfileEntity>(HttpMethod.Get, "/profile");

	public Task UpdateProfile(UserProfileEntity request) =>
		api.Call(HttpMethod.Put, "/profile", data: request);
}