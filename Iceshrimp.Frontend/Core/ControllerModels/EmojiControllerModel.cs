using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Helpers;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components.Forms;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class EmojiControllerModel(ApiClient api)
{
	public Task<List<EmojiResponse>> GetAllEmojiAsync() =>
		api.CallAsync<List<EmojiResponse>>(HttpMethod.Get, "/emoji");

	public Task<PaginationWrapper<List<EmojiResponse>>> GetRemoteEmojiAsync(PaginationQuery pq) =>
		api.CallAsync<PaginationWrapper<List<EmojiResponse>>>(HttpMethod.Get, "/emoji/remote", pq);

	public Task<PaginationWrapper<List<EmojiResponse>>> GetRemoteEmojiAsync(string instance, PaginationQuery pq) =>
		api.CallAsync<PaginationWrapper<List<EmojiResponse>>>(HttpMethod.Get, $"/emoji/remote/{instance}", pq);

	public Task<List<EntityWrapper<string>>> GetRemoteEmojiHostsAsync(PaginationQuery pq) =>
		api.CallAsync<List<EntityWrapper<string>>>(HttpMethod.Get, "/emoji/remote/hosts", pq);

	public Task<EmojiResponse> UploadEmojiAsync(IBrowserFile file, string? name) =>
		api.CallAsync<EmojiResponse>(HttpMethod.Post, "/emoji",
		                             name != null ? QueryString.Create("name", name) : QueryString.Empty, file);

	public Task<EmojiResponse?> CloneEmojiAsync(string name, string host) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Post, $"/emoji/clone/{name}@{host}");

	public Task<bool> ImportEmojiAsync(IBrowserFile file) =>
		api.CallNullableAsync(HttpMethod.Post, "/emoji/import", data: file);

	public Task<EmojiResponse?> UpdateEmojiAsync(string id, UpdateEmojiRequest request) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Patch, $"/emoji/{id}", data: request);

	public Task<bool> DeleteEmojiAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/emoji/{id}");

	public Task<EmojiResponse?> GetEmojiAsync(string id) =>
		api.CallNullableAsync<EmojiResponse>(HttpMethod.Get, $"/emoji/{id}");
}
