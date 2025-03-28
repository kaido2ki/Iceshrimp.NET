using System.Text.Json.Nodes;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AdminControllerModel(ApiClient api)
{
	public Task<InviteResponse> GenerateInviteAsync() =>
		api.CallAsync<InviteResponse>(HttpMethod.Post, "/invites/generate");

	public Task<JsonObject?> GetActivityByNoteIdAsync(string id) =>
		api.CallNullableAsync<JsonObject>(HttpMethod.Get, $"/admin/activities/notes/{id}");

	public Task<JsonObject?> GetActivityByUserIdAsync(string id) =>
		api.CallNullableAsync<JsonObject>(HttpMethod.Get, $"/admin/activities/users/{id}");

	//TODO: other endpoints
}