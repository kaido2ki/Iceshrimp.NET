using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class FollowRequestControllerModel(ApiClient api)
{
	public Task<List<FollowRequestResponse>> GetFollowRequests(PaginationQuery pq) =>
		api.Call<List<FollowRequestResponse>>(HttpMethod.Get, "/follow_requests", pq);

	public Task AcceptFollowRequest(string id) => api.Call(HttpMethod.Post, $"/follow_requests/{id}/accept");
	public Task RejectFollowRequest(string id) => api.Call(HttpMethod.Post, $"/follow_requests/{id}/reject");
}