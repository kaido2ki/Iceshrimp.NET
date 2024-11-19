using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SearchControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> SearchNotesAsync(string query, PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/search/notes", QueryString.Create("q", query) + pq);

	[LinkPagination(20, 80)]
	public Task<List<UserResponse>> SearchUsersAsync(string query, PaginationQuery pq) =>
		api.CallAsync<List<UserResponse>>(HttpMethod.Get, "/search/users", QueryString.Create("q", query) + pq);

	public Task<RedirectResponse?> LookupAsync(string target) =>
		api.CallNullableAsync<RedirectResponse>(HttpMethod.Get, "/search/lookup", QueryString.Create("target", target));
}