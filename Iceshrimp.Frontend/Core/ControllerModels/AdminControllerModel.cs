using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AdminControllerModel(ApiClient api)
{
	public Task<InviteResponse> GenerateInvite() =>
		api.Call<InviteResponse>(HttpMethod.Post, "/invites/generate");
	
	//TODO: ActivityStreams debug endpoints
}