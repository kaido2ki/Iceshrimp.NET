using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class MigrationControllerModel(ApiClient api)
{
    public Task<MigrationSchemas.MigrationStatusResponse> GetMigrationStatusAsync() =>
        api.CallAsync<MigrationSchemas.MigrationStatusResponse>(HttpMethod.Get, "/migration");

    public Task AddAliasAsync(MigrationSchemas.MigrationRequest request) =>
        api.CallAsync(HttpMethod.Post, "/migration/aliases", data: request);

    public Task RemoveAliasAsync(MigrationSchemas.MigrationRequest request) =>
        api.CallAsync(HttpMethod.Delete, "/migration/aliases", data: request);
}
