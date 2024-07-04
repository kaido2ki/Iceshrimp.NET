using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Http;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SearchControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> SearchNotes(string query, PaginationQuery pq) =>
		api.Call<List<NoteResponse>>(HttpMethod.Get, "/search/notes", QueryString.Create("q", query) + pq);

	[LinkPagination(20, 80)]
	public Task<List<UserResponse>> SearchUsers(string query, PaginationQuery pq) =>
		api.Call<List<UserResponse>>(HttpMethod.Get, "/search/users", QueryString.Create("q", query) + pq);

	public Task<RedirectResponse?> Lookup(string target) =>
		api.CallNullable<RedirectResponse>(HttpMethod.Get, "/search/lookup", QueryString.Create("target", target));
}