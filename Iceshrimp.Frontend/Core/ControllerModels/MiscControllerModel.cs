using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class MiscControllerModel(ApiClient api)
{
	public Task<IEnumerable<NoteResponse>> GetMutedThreadsAsync(PaginationQuery pq) =>
		api.CallAsync<IEnumerable<NoteResponse>>(HttpMethod.Get, "/misc/muted_threads", pq);
}