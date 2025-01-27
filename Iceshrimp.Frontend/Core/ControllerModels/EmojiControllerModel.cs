using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class EmojiControllerModel(ApiClient api)
{
	public Task<List<EmojiResponse>> GetAllEmojiAsync() =>
		api.CallAsync<List<EmojiResponse>>(HttpMethod.Get, "/emoji");

	public Task<List<EmojiResponse>> GetRemoteEmojiAsync() =>
		api.CallAsync<List<EmojiResponse>>(HttpMethod.Get, "/emoji/remote");

	public Task<EmojiResponse> UploadEmojiAsync(IBrowserFile file) =>
		api.CallAsync<EmojiResponse>(HttpMethod.Post, "/emoji", data: file);

	public Task<EmojiResponse?> CloneEmojiAsync(string name, string host) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Post, $"/emoji/clone/{name}@{host}");

	public Task<EmojiResponse?> UpdateEmojiAsync(string id, UpdateEmojiRequest request) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Patch, $"/emoji/{id}", data: request);

	public Task<bool> DeleteEmojiAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/emoji/{id}");

	public Task<EmojiResponse?> GetEmojiAsync(string id) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Get, $"/emoji/{id}");
}