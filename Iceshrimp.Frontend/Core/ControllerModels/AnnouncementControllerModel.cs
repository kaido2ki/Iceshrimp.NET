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

    public Task<AnnouncementResponse> CreateAnnouncementAsync(AnnouncementRequest request) =>
        api.CallAsync<AnnouncementResponse>(HttpMethod.Post, "/announcements", data: request);

    public Task<AnnouncementResponse> EditAnnouncementAsync(string id, AnnouncementRequest request) =>
        api.CallAsync<AnnouncementResponse>(HttpMethod.Put, $"/announcements/{id}", data: request);

    public Task DeleteAnnouncementAsync(string id) =>
        api.CallAsync(HttpMethod.Delete, $"/announcements/{id}");

    public Task ReadAnnouncementAsync(string id) =>
        api.CallAsync(HttpMethod.Post, $"/announcements/{id}/read");
}
