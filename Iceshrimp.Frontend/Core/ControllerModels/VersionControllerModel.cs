using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class VersionControllerModel(ApiClient api)
{
	public Task<VersionResponse> GetVersionAsync() =>
		api.CallAsync<VersionResponse>(HttpMethod.Get, "/version");
}