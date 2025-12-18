using Iceshrimp.Backend.Components.Helpers;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

public partial class FederationList(IOptionsSnapshot<Config.SecuritySection> security) : AsyncComponentBase
{
    [CascadingParameter(Name = "InstanceName")]
    public required string? InstanceName { get; set; }

    private bool                       IsAllowList    { get; set; }
    private bool                       IsLoggedIn     { get; set; }
    private bool                       IsAdmin        { get; set; }
    private Enums.ItemVisibility       DisplayList    { get; set; }
    private Enums.ItemVisibility       DisplayReasons { get; set; }
    private bool                       HideReasons    { get; set; }
    private Dictionary<string, string> List           { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        if (Context.Request.Cookies.TryGetValue("sessions", out var sessions))
        {
            var tokens = sessions.Split('|');
            if (await Database.Sessions.AnyAsync(p => tokens.Contains(p.Token)))
            {
                IsLoggedIn = true;
            }
        }

        if (Context.Request.Cookies.TryGetValue("admin_session", out var admSession)
            && await Database.Sessions.AnyAsync(p => p.Token == admSession && p.Active && p.User.IsAdmin))
        {
            IsAdmin = true;
        }

        IsAllowList    = security.Value.FederationMode == Enums.FederationMode.AllowList;
        DisplayList    = security.Value.ExposeFederationList;
        DisplayReasons = security.Value.ExposeBlockReasons;

        if ((DisplayList == Enums.ItemVisibility.Registered && !IsLoggedIn)
            || (DisplayList == Enums.ItemVisibility.Hide && !IsAdmin))
            return;

        if ((DisplayReasons == Enums.ItemVisibility.Registered && !IsLoggedIn)
            || (DisplayReasons == Enums.ItemVisibility.Hide && !IsAdmin))
            HideReasons = true;

        if (IsAllowList)
        {
            List = await Database.AllowedInstances
                                 .OrderBy(p => p.Host)
                                 .ToDictionaryAsync(p => p.Host, _ => "");
        }
        else
        {
            List = await Database.BlockedInstances
                                 .OrderBy(p => p.Host)
                                 .ToDictionaryAsync(p => p.Host, p => p.Reason ?? "");
        }
    }
}
