using System.Net;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class ModerationControllerModel(ApiClient api)
{
    public Task SuspendUserAsync(string id) =>
        api.CallNullableAsync(HttpMethod.Post, $"/moderation/users/{id}/suspend");

    [LinkPagination(20, 40)]
    public Task<List<ReportResponse>> GetReportsAsync(PaginationQuery pq, bool resolved = false) =>
        api.CallAsync<List<ReportResponse>>(HttpMethod.Get, "/moderation/reports",
                                            pq + QueryString.Create("resolved", resolved ? "true" : "false"));

    public Task<ReportResponse?> GetReportAsync(string id) =>
        api.CallNullableAsync<ReportResponse>(HttpMethod.Get, $"/moderation/reports/{id}");

    public Task ResolveReportAsync(string id) =>
        api.CallAsync(HttpMethod.Post, $"/moderation/reports/{id}/resolve");

    public Task ForwardReportAsync(string id) =>
        api.CallAsync(HttpMethod.Post, $"/moderation/reports/{id}/forward");

    public Task DeleteReportAsync(string id) =>
        api.CallAsync(HttpMethod.Delete, $"/moderation/reports/{id}");
}
