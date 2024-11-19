using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class UserControllerModel(ApiClient api)
{
	public Task<UserResponse?> GetUserAsync(string id) =>
		api.CallNullableAsync<UserResponse>(HttpMethod.Get, $"/users/{id}");

	public Task<UserProfileResponse?> GetUserProfileAsync(string id) =>
		api.CallNullableAsync<UserProfileResponse>(HttpMethod.Get, $"/users/{id}/profile");

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>?> GetUserNotesAsync(string id, PaginationQuery pq) =>
		api.CallNullableAsync<List<NoteResponse>>(HttpMethod.Get, $"/users/{id}/notes", pq);

	public Task<UserResponse?> LookupUserAsync(string username, string? host)
	{
		var query = new QueryString();
		query = query.Add("username", username);
		if (host != null) query = query.Add("host", host);
		return api.CallNullableAsync<UserResponse>(HttpMethod.Get, "/users/lookup", query);
	}

	public Task BiteUserAsync(string id) =>
		api.CallAsync(HttpMethod.Post, $"/users/{id}/bite");
	
	public Task<bool> FollowUserAsync(string id) => api.CallNullableAsync(HttpMethod.Post, $"/users/{id}/follow");
	public Task<bool> RemoveUserFromFollowersAsync(string id) => api.CallNullableAsync(HttpMethod.Post, $"/users/{id}/remove_from_followers");
	public Task<bool> UnfollowUserAsync(string id) => api.CallNullableAsync(HttpMethod.Post, $"/users/{id}/unfollow");
}