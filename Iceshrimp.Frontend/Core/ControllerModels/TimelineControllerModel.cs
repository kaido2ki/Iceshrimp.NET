using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class TimelineControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<PaginationWrapper<NoteResponse>> GetHomeTimeline(string? cursor = null)
	{
		var query                 = new QueryString();
		if (cursor != null) query = query.Add("cursor", cursor);
		return api.Call<PaginationWrapper<NoteResponse>>(HttpMethod.Get, "/timelines/home", query);
	}
}
