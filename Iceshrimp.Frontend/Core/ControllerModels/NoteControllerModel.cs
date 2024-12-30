using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NoteControllerModel(ApiClient api)
{
	public Task<NoteResponse?> GetNoteAsync(string id) =>
		api.CallNullableAsync<NoteResponse>(HttpMethod.Get, $"/notes/{id}");

	public Task<bool> DeleteNoteAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Delete, $"/notes/{id}");

	public Task<List<NoteResponse>?> GetNoteAscendantsAsync(string id, [DefaultValue(20)] [Range(1, 100)] int? limit)
	{
		var query = new QueryString();
		if (limit.HasValue) query.Add("limit", limit.Value.ToString());
		return api.CallNullableAsync<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/ascendants", query);
	}

	public Task<List<NoteResponse>?> GetNoteDescendantsAsync(string id, [DefaultValue(20)] [Range(1, 100)] int? depth)
	{
		var query = new QueryString();
		if (depth.HasValue) query.Add("depth", depth.Value.ToString());
		return api.CallNullableAsync<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/descendants", query);
	}

	public Task<List<UserResponse>?> GetNoteReactionsAsync(string id, string name) =>
		api.CallNullableAsync<List<UserResponse>>(HttpMethod.Get, $"/notes/{id}/reactions/{name}");

	public Task BiteNoteAsync(string id) =>
		api.CallAsync(HttpMethod.Post, $"/notes/{id}/bite");

	public Task<ValueResponse?> LikeNoteAsync(string id) =>
		api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/like");

	public Task<ValueResponse?> UnlikeNoteAsync(string id) =>
		api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unlike");

	public Task<PaginationWrapper<List<UserResponse>>?> GetNoteLikesAsync(string id, PaginationQuery pq) =>
		api.CallNullableAsync<PaginationWrapper<List<UserResponse>>>(HttpMethod.Get, $"/notes/{id}/likes", pq);

	public Task<ValueResponse?> RenoteNoteAsync(string id, NoteVisibility? visibility = null)
	{
		var query = new QueryString();
		if (visibility.HasValue) query.Add("visibility", ((int)visibility.Value).ToString().ToLowerInvariant());
		return api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/renote", query);
	}

	public Task<ValueResponse?> UnrenoteNoteAsync(string id) =>
		api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unrenote");

	public Task<PaginationWrapper<List<UserResponse>>?> GetRenotesAsync(string id, PaginationQuery pq) =>
		api.CallNullableAsync<PaginationWrapper<List<UserResponse>>>(HttpMethod.Get, $"/notes/{id}/renotes", pq);

	public Task<PaginationWrapper<List<NoteResponse>>?> GetQuotesAsync(string id, PaginationQuery pq) =>
		api.CallNullableAsync<PaginationWrapper<List<NoteResponse>>>(HttpMethod.Get, $"/notes/{id}/quotes");

	public Task<ValueResponse?> ReactToNoteAsync(string id, string name) =>
		api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/react/{name}");

	public Task<ValueResponse?> RemoveReactionFromNoteAsync(string id, string name) =>
		api.CallNullableAsync<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unreact/{name}");

	public Task<NoteResponse> CreateNoteAsync(NoteCreateRequest request) =>
		api.CallAsync<NoteResponse>(HttpMethod.Post, "/notes", data: request);

	public Task<NoteRefetchResponse?> RefetchNoteAsync(string id) =>
		api.CallNullableAsync<NoteRefetchResponse>(HttpMethod.Get, $"/notes/{id}/refetch");

	public Task<NotePollSchema?> AddPollVoteAsync(string id, List<int> choices) =>
		api.CallNullableAsync<NotePollSchema>(HttpMethod.Post, $"/notes/{id}/vote",
		                                      data: new NotePollRequest { Choices = choices });

	public Task MuteNoteAsync(string id) =>
		api.CallAsync(HttpMethod.Post, $"/notes/{id}/mute");
}