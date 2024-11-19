using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class TimelineControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetHomeTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/home", pq);
}