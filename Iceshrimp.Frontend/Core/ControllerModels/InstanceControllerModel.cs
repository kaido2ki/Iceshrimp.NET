using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class InstanceControllerModel(ApiClient api)
{
    public Task<InstanceResponse> GetInstance() =>
        api.CallAsync<InstanceResponse>(HttpMethod.Get, "/instance");
}