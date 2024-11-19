using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class EmojiControllerModel(ApiClient api)
{
	public Task<List<EmojiResponse>> GetAllEmojiAsync() =>
		api.CallAsync<List<EmojiResponse>>(HttpMethod.Get, "/emoji");

	public Task<EmojiResponse> UploadEmojiAsync(IBrowserFile file) =>
		api.CallAsync<EmojiResponse>(HttpMethod.Post, "/emoji", data: file);

	public Task<EmojiResponse?> UpdateEmojiAsync(string id, EmojiResponse emoji) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Patch, $"/emoji/{id}", data: emoji);

	public Task<EmojiResponse?> GetEmojiAsync(string id) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Get, $"/emoji/{id}");
}