using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NoteControllerModel(ApiClient api)
{
	public Task<NoteResponse?> GetNote(string id) =>
		api.CallNullable<NoteResponse>(HttpMethod.Get, $"/notes/{id}");

	public Task<bool> DeleteNote(string id) =>
		api.CallNullable(HttpMethod.Delete, $"/notes/{id}");

	public Task<List<NoteResponse>?> GetNoteAscendants(string id, [DefaultValue(20)] [Range(1, 100)] int? limit)
	{
		var query = new QueryString();
		if (limit.HasValue) query.Add("limit", limit.Value.ToString());
		return api.CallNullable<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/ascendants", query);
	}

	public Task<List<NoteResponse>?> GetNoteDescendants(string id, [DefaultValue(20)] [Range(1, 100)] int? depth)
	{
		var query = new QueryString();
		if (depth.HasValue) query.Add("depth", depth.Value.ToString());
		return api.CallNullable<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/descendants", query);
	}

	public Task<List<UserResponse>?> GetNoteReactions(string id, string name) =>
		api.CallNullable<List<UserResponse>>(HttpMethod.Get, $"/notes/{id}/reactions/{name}");

	public Task BiteNote(string id) =>
		api.Call(HttpMethod.Post, $"/notes/{id}/bite");
	
	public Task<ValueResponse?> LikeNote(string id) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/like");

	public Task<ValueResponse?> UnlikeNote(string id) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unlike");

	public Task<PaginationWrapper<List<UserResponse>>?> GetNoteLikes(string id, PaginationQuery pq) =>
		api.CallNullable<PaginationWrapper<List<UserResponse>>>(HttpMethod.Get, $"/notes/{id}/likes", pq);

	public Task<ValueResponse?> RenoteNote(string id, NoteVisibility? visibility = null)
	{
		var query = new QueryString();
		if (visibility.HasValue) query.Add("visibility", ((int)visibility.Value).ToString().ToLowerInvariant());
		return api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/renote", query);
	}

	public Task<ValueResponse?> UnrenoteNote(string id) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unrenote");

	public Task<PaginationWrapper<List<UserResponse>>?> GetRenotes(string id, PaginationQuery pq) =>
		api.CallNullable<PaginationWrapper<List<UserResponse>>>(HttpMethod.Get, $"/notes/{id}/renotes", pq);

	public Task<PaginationWrapper<List<NoteResponse>>?> GetQuotes(string id, PaginationQuery pq) =>
		api.CallNullable<PaginationWrapper<List<NoteResponse>>>(HttpMethod.Get, $"/notes/{id}/quotes");

	public Task<ValueResponse?> ReactToNote(string id, string name) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/react/{name}");

	public Task<ValueResponse?> RemoveReactionFromNote(string id, string name) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unreact/{name}");

	public Task<NoteResponse> CreateNote(NoteCreateRequest request) =>
		api.Call<NoteResponse>(HttpMethod.Post, "/notes", data: request);

	public Task<NoteRefetchResponse?> RefetchNote(string id) =>
		api.CallNullable<NoteRefetchResponse>(HttpMethod.Get, $"/notes/{id}/refetch");

	public Task MuteNote(string id) =>
		api.Call(HttpMethod.Post, $"/notes/{id}/mute");
}