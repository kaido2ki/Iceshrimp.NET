using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class MiscControllerModel(ApiClient api)
{
	public Task BiteBackAsync(string id) =>
		api.CallAsync(HttpMethod.Post, $"/misc/bite_back/{id}");

	public Task<IEnumerable<NoteResponse>> GetMutedThreadsAsync(PaginationQuery pq) =>
		api.CallAsync<IEnumerable<NoteResponse>>(HttpMethod.Get, "/misc/muted_threads", pq);

	public Task<StatusResponse?> GetStatusAsync() =>
		api.CallNullableAsync<StatusResponse>(HttpMethod.Get, "/misc/status");
}