using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AdminControllerModel(ApiClient api)
{
	public Task<InviteResponse> GenerateInvite() =>
		api.Call<InviteResponse>(HttpMethod.Post, "/invites/generate");

	//TODO: ActivityStreams debug endpoints
	//TODO: other endpoints
}