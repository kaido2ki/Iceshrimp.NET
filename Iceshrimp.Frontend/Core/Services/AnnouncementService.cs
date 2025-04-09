using Iceshrimp.Frontend.Components;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services;

internal class AnnouncementService
{
    public  AnnouncementsDialog? AnnouncementsDialog { get; set; }
    private UpdateService        _update;
    private Timer                _timer;

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
}
