using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class FollowRequestControllerModel(ApiClient api)
{
	public Task<List<FollowRequestResponse>> GetFollowRequestsAsync(PaginationQuery pq) =>
		api.CallAsync<List<FollowRequestResponse>>(HttpMethod.Get, "/follow_requests", pq);

	public Task<bool> AcceptFollowRequestAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Post, $"/follow_requests/{id}/accept");

	public Task<bool> RejectFollowRequestAsync(string id) =>
		api.CallNullableAsync(HttpMethod.Post, $"/follow_requests/{id}/reject");
}