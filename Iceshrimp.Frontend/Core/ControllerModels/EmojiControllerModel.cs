using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class EmojiControllerModel(ApiClient api)
{
	public Task<List<EmojiResponse>> GetAllEmoji() =>
		api.Call<List<EmojiResponse>>(HttpMethod.Get, "/emoji");

	public Task<EmojiResponse> UploadEmoji(IBrowserFile file) =>
		api.Call<EmojiResponse>(HttpMethod.Post, "/emoji", data: file);

	public Task<EmojiResponse?> UpdateEmoji(string id, EmojiResponse emoji) =>
		api.CallNullable<EmojiResponse>(HttpMethod.Patch, $"/emoji/{id}", data: emoji);

	public Task<EmojiResponse?> GetEmoji(string id) =>
		api.CallNullable<EmojiResponse>(HttpMethod.Get, $"/emoji/{id}");
}