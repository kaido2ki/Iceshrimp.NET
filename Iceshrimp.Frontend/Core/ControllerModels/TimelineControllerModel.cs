using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class TimelineControllerModel(ApiClient api)
{
	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetHomeTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/home", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetLocalTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/local", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetSocialTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/social", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetBubbleTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/bubble", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetGlobalTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/global", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetBookmarksTimelineAsync(PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, "/timelines/bookmarks", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetListTimelineAsync(string listId, PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, $"/timelines/list/{listId}", pq);

	[LinkPagination(20, 80)]
	public Task<List<NoteResponse>> GetRemoteTimelineAsync(string instance, PaginationQuery pq) =>
		api.CallAsync<List<NoteResponse>>(HttpMethod.Get, $"/timelines/remote/{instance}", pq);
}
