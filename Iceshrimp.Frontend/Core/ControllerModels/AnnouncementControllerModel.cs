using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class AnnouncementControllerModel(ApiClient api)
{
    public Task<PaginationWrapper<List<AnnouncementResponse>>> GetAnnouncementsAsync(bool popups, PaginationQuery pq) =>
        api.CallAsync<PaginationWrapper<List<AnnouncementResponse>>>(HttpMethod.Get, "/announcements",
                                                                     QueryString.Create("popups", popups ? "true" : "false")
                                                                     + pq);

    public Task ReadAnnouncementAsync(string id) =>
        api.CallAsync(HttpMethod.Post, $"/announcements/{id}/read");
}
