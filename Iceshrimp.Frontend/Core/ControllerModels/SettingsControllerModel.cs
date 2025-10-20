using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SettingsControllerModel(ApiClient api)
{
	public Task<UserSettingsResponse> GetSettingsAsync() =>
		api.CallAsync<UserSettingsResponse>(HttpMethod.Get, "/settings");

	public Task<bool> UpdateSettingsAsync(UserSettingsRequest settings) =>
		api.CallNullableAsync(HttpMethod.Put, "/settings", data: settings);

	public Task<TwoFactorEnrollmentResponse> EnrollTwoFactorAsync() =>
		api.CallAsync<TwoFactorEnrollmentResponse>(HttpMethod.Post, "settings/2fa/enroll");

	public Task ConfirmTwoFactorAsync(TwoFactorRequest request) =>
		api.CallAsync(HttpMethod.Post, "settings/2fa/confirm", data: request);

	public Task DisableTwoFactorAsync(TwoFactorRequest request) =>
		api.CallAsync(HttpMethod.Post, "settings/2fa/disable", data: request);

	public Task<DriveFileResponse> ExportBlockingAsync() =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Post, "settings/export/blocking");

	public Task<DriveFileResponse> ExportFollowingAsync() =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Post, "settings/export/following");

	public Task ImportBlockingAsync(IBrowserFile file) =>
		api.CallAsync(HttpMethod.Post, "settings/import/blocking", data: file);

	public Task ImportFollowingAsync(IBrowserFile file) =>
		api.CallAsync(HttpMethod.Post, "settings/import/following", data: file);
}