using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Http;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class NoteControllerModel(ApiClient api)
{
	public Task<NoteResponse?> GetNote(string id) =>
		api.CallNullable<NoteResponse>(HttpMethod.Get, $"/notes/{id}");

	public Task<List<NoteResponse>?> GetNoteAscendants(string id, [DefaultValue(20)] [Range(1, 100)] int? limit)
	{
		var query = new QueryString();
		if (limit.HasValue) query.Add("limit", limit.ToString());
		return api.CallNullable<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/ascendants", query);
	}

	public Task<List<NoteResponse>?> GetNoteDescendants(string id, [DefaultValue(20)] [Range(1, 100)] int? depth)
	{
		var query = new QueryString();
		if (depth.HasValue) query.Add("depth", depth.ToString());
		return api.CallNullable<List<NoteResponse>>(HttpMethod.Get, $"/notes/{id}/descendants", query);
	}

	public Task<List<UserResponse>?> GetNoteReactions(string id, string name) =>
		api.CallNullable<List<UserResponse>>(HttpMethod.Get, $"/notes/{id}/reactions/{name}");

	public Task<ValueResponse?> LikeNote(string id) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/like");

	public Task<ValueResponse?> UnlikeNote(string id) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unlike");

	public Task<ValueResponse?> ReactToNote(string id, string name) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/react/{name}");

	public Task<ValueResponse?> RemoveReactionFromNote(string id, string name) =>
		api.CallNullable<ValueResponse>(HttpMethod.Post, $"/notes/{id}/unreact/{name}");

	public Task<NoteResponse> CreateNote(NoteCreateRequest request) =>
		api.Call<NoteResponse>(HttpMethod.Post, "/notes", data: request);
}