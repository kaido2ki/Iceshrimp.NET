using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class ProfileControllerModel(ApiClient api)
{
	public Task<UserProfileEntity> GetProfileAsync() =>
		api.CallAsync<UserProfileEntity>(HttpMethod.Get, "/profile");

	public Task UpdateProfileAsync(UserProfileEntity request) =>
		api.CallAsync(HttpMethod.Put, "/profile", data: request);

	public Task<DriveFileResponse> GetAvatarAsync() =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Get, "/profile/avatar");

	public Task UpdateAvatarAsync(IBrowserFile file) =>
		api.CallAsync(HttpMethod.Post, "/profile/avatar", data: file);

	public Task DeleteAvatarAsync() =>
		api.CallAsync(HttpMethod.Delete, "/profile/avatar");

	public Task<DriveFileResponse> GetBannerAsync() =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Get, "/profile/banner");

	public Task UpdateBannerAsync(IBrowserFile file) =>
		api.CallAsync(HttpMethod.Post, "/profile/banner", data: file);

	public Task DeleteBannerAsync() =>
		api.CallAsync(HttpMethod.Delete, "/profile/banner");
}