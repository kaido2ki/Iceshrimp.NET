using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class InstanceControllerModel(ApiClient api)
{
    public Task<InstanceResponse> GetInstanceAsync() =>
        api.CallAsync<InstanceResponse>(HttpMethod.Get, "/instance");
}