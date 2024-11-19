using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class DriveControllerModel(ApiClient api)
{
	public Task<DriveFileResponse> UploadFileAsync(IBrowserFile file) =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Post, "/drive", data: file);

	public Task<DriveFileResponse?> GetFileAsync(string id) =>
		api.CallNullableAsync<DriveFileResponse>(HttpMethod.Get, $"/drive/{id}");

	public Task<DriveFileResponse?> UpdateFileAsync(string id, UpdateDriveFileRequest request) =>
		api.CallNullableAsync<DriveFileResponse>(HttpMethod.Patch, $"/drive/{id}", data: request);
}