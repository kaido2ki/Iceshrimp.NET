using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class TimelineControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetHomeTimeline(PaginationQuery pq) =>
		api.Call<List<NoteResponse>>(HttpMethod.Get, "/timelines/home", pq);
}