using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class MiscControllerModel(ApiClient api)
{
	public Task<IEnumerable<NoteResponse>> GetMutedThreads(PaginationQuery pq) =>
		api.Call<IEnumerable<NoteResponse>>(HttpMethod.Get, "/misc/muted_threads", pq);
}