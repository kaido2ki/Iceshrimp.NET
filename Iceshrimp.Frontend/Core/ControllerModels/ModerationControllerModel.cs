using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class ModerationControllerModel(ApiClient api)
{
    [LinkPagination(20, 40)]
    public Task<List<ReportResponse>> GetReportsAsync(PaginationQuery pq, bool resolved = false) =>
        api.CallAsync<List<ReportResponse>>(HttpMethod.Get, "/moderation/reports",
                                            pq + QueryString.Create("resolved", resolved ? "true" : "false"));

    public Task ResolveReportAsync(string id) =>
        api.CallAsync(HttpMethod.Post, $"/moderation/reports/{id}/resolve");
}
