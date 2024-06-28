using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class DriveControllerModel(ApiClient api)
{
	public Task<DriveFileResponse> UploadFile(IBrowserFile file) =>
		api.Call<DriveFileResponse>(HttpMethod.Post, "/drive", data: file);

	public Task<DriveFileResponse?> GetFile(string id) =>
		api.CallNullable<DriveFileResponse>(HttpMethod.Get, $"/drive/{id}");

	public Task<DriveFileResponse?> UpdateFile(string id, UpdateDriveFileRequest request) =>
		api.CallNullable<DriveFileResponse>(HttpMethod.Patch, $"/drive/{id}", data: request);
}