using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class VersionControllerModel(ApiClient api)
{
	public Task<VersionResponse> GetVersion() =>
		api.Call<VersionResponse>(HttpMethod.Get, "/version");
}