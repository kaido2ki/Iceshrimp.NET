using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class MetadataService
{
    [Inject] private ApiService                   Api      { get; set; }
    public           Lazy<Task<InstanceResponse>> Instance { get; set; }

    public MetadataService(ApiService api)
    {
        Api      = api;
        Instance = new Lazy<Task<InstanceResponse>>(() => Api.Instance.GetInstance());
    }
}