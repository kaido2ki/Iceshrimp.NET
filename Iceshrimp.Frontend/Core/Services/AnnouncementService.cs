using Iceshrimp.Frontend.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class AnnouncementService : IDisposable, IAsyncDisposable
{
    public           AnnouncementsDialog? AnnouncementsDialog { get; set; }
    private readonly UpdateService        _update;
    private          Timer                _timer;

    public AnnouncementService(UpdateService updateService)
    {
        _update = updateService;
        _timer  = new Timer(CheckAnnouncementsCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
    }

    private void CheckAnnouncementsCallback(object? caller)
    {
        _ = CheckAnnouncementsAsync();
    }

    private async Task CheckAnnouncementsAsync()
    {
        // Don't show announcements if updates are available
        if (_update.DialogOpen) return;
        await AnnouncementsDialog?.Display()!;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
    }
}
