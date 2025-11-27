using Iceshrimp.Backend.Components.Helpers;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Pages.Queue;

public partial class QueueJob : AdminComponentBase
{
    [Parameter] public required Guid Id { get; set; }

    private Job JobDetails { get; set; } = null!;

    public static Dictionary<string, string> Lookup = new()
    {
        ["inbox"]       = "body",
        ["deliver"]     = "payload",
        ["pre-deliver"] = "serializedActivity"
    };

    protected override async Task OnInitializedAsync()
    {
        JobDetails = await Database.Jobs.FirstOrDefaultAsync(p => p.Id == Id) ??
                     throw GracefulException.NotFound($"Job {Id} not found");
    }
}

