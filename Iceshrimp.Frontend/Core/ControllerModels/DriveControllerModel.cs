using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class DriveControllerModel(ApiClient api)
{
	public Task<DriveFileResponse> UploadFileAsync(IBrowserFile file) =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Post, "/drive", data: file);

	public Task<DriveFileResponse> UploadFileToFolderAsync(IBrowserFile file, string folderId) =>
		api.CallAsync<DriveFileResponse>(HttpMethod.Post, "/drive", QueryString.Create("folderId", folderId), file);

	public Task<DriveFileResponse?> GetFileAsync(string id) =>
		api.CallNullableAsync<DriveFileResponse>(HttpMethod.Get, $"/drive/{id}");

	public Task<DriveFileResponse?> UpdateFileAsync(string id, UpdateDriveFileRequest request) =>
		api.CallNullableAsync<DriveFileResponse>(HttpMethod.Patch, $"/drive/{id}", data: request);

	public Task DeleteFileAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/drive/{id}");

	public Task<DriveFolderResponse?> GetFolderAsync(string? id) =>
		api.CallNullableAsync<DriveFolderResponse>(HttpMethod.Get, "/drive/folder" + (id != null ? $"/{id}" : ""));

	public Task<DriveFolderResponse?> UpdateFolderAsync(string id, string name) =>
		api.CallNullableAsync<DriveFolderResponse>(HttpMethod.Put, $"/drive/folder/{id}", QueryString.Create("name", name));

	public Task DeleteFolderAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/drive/folder/{id}");
}