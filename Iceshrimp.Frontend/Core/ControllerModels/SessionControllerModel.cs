using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.ControllerModels;

internal class SessionControllerModel(ApiClient api)
{
    public Task<List<SessionSchemas.SessionResponse>> GetSessionsAsync() =>
        api.CallAsync<List<SessionSchemas.SessionResponse>>(HttpMethod.Get, "sessions");

    public Task TerminateAllSessionsAsync() =>
        api.CallAsync(HttpMethod.Delete, "sessions/all");

    public Task TerminateSessionAsync(string id) =>
        api.CallAsync(HttpMethod.Delete, $"sessions/{id}");

    public Task<List<SessionSchemas.MastodonSessionResponse>> GetMastodonSessionsAsync() =>
        api.CallAsync<List<SessionSchemas.MastodonSessionResponse>>(HttpMethod.Get, "sessions/mastodon");

    public Task UpdateMastodonSessionAsync(string id, SessionSchemas.MastodonSessionFlags flags) =>
        api.CallAsync(HttpMethod.Patch, $"sessions/mastodon/{id}", data: flags);

    public Task TerminateMastodonSessionAsync(string id) =>
        api.CallAsync(HttpMethod.Delete, $"sessions/mastodon/{id}");

    public Task<SessionSchemas.CreatedMastodonSessionResponse> CreateMastodonSessionAsync(
        SessionSchemas.MastodonSessionRequest request
    ) =>
        api.CallAsync<SessionSchemas.CreatedMastodonSessionResponse>(HttpMethod.Post, "sessions/mastodon",
                                                                     data: request);
}
