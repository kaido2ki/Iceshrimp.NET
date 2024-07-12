using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class FilterControllerModel(ApiClient api)
{
	public Task<IEnumerable<FilterResponse>> GetFilters() =>
		api.Call<IEnumerable<FilterResponse>>(HttpMethod.Get, "/filters");

	public Task<FilterResponse> CreateFilter(FilterRequest request) =>
		api.Call<FilterResponse>(HttpMethod.Post, "/filters", data: request);

	public Task UpdateFilter(long id, FilterRequest request) =>
		api.Call(HttpMethod.Put, $"/filters/{id}", data: request);

	public Task DeleteFilter(long id) => api.Call(HttpMethod.Delete, $"/filters/{id}");
}