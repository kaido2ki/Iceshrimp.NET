using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class FollowRequestControllerModel(ApiClient api)
{
	public Task<List<FollowRequestResponse>> GetFollowRequests(PaginationQuery pq) =>
		api.Call<List<FollowRequestResponse>>(HttpMethod.Get, "/follow_requests", pq);

	public Task<bool> AcceptFollowRequest(string id) =>
		api.CallNullable(HttpMethod.Post, $"/follow_requests/{id}/accept");

	public Task<bool> RejectFollowRequest(string id) =>
		api.CallNullable(HttpMethod.Post, $"/follow_requests/{id}/reject");
}