using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class FilterControllerModel(ApiClient api)
{
	public Task<IEnumerable<FilterResponse>> GetFiltersAsync() =>
		api.CallAsync<IEnumerable<FilterResponse>>(HttpMethod.Get, "/filters");

	public Task<FilterResponse> CreateFilterAsync(FilterRequest request) =>
		api.CallAsync<FilterResponse>(HttpMethod.Post, "/filters", data: request);

	public Task<bool> UpdateFilterAsync(long id, FilterRequest request) =>
		api.CallNullableAsync(HttpMethod.Put, $"/filters/{id}", data: request);

	public Task<bool> DeleteFilterAsync(long id) => api.CallNullableAsync(HttpMethod.Delete, $"/filters/{id}");
}