using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class UserControllerModel(ApiClient api)
{
	public Task<UserResponse?> GetUser(string id) =>
		api.CallNullable<UserResponse>(HttpMethod.Get, $"/users/{id}");

	public Task<UserProfileResponse?> GetUserProfile(string id) =>
		api.CallNullable<UserProfileResponse>(HttpMethod.Get, $"/users/{id}/profile");

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>?> GetUserNotes(string id, PaginationQuery pq) =>
		api.CallNullable<List<NoteResponse>>(HttpMethod.Get, $"/users/{id}/notes", pq);

	public Task<UserResponse?> LookupUser(string username, string? host)
	{
		var query = new QueryString();
		query = query.Add("username", username);
		if (host != null) query = query.Add("host", host);
		return api.CallNullable<UserResponse>(HttpMethod.Get, "/users/lookup", query);
	}

	public Task BiteUser(string id) =>
		api.Call(HttpMethod.Post, $"/users/{id}/bite");
	
	public Task<bool> FollowUser(string id)   => api.CallNullable(HttpMethod.Post, $"/users/{id}/follow");
	public Task<bool> UnfollowUser(string id) => api.CallNullable(HttpMethod.Post, $"/users/{id}/unfollow");
}