using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Pages;

[Authenticate]
public class FederationList (
    DatabaseContext db,
    IOptionsSnapshot<Config.SecuritySection> security
) : PageModel
{
    public bool    IsBlocklist;
    public string? ModeString;

    public bool ListRegOnly;
    public bool ReasonsRegOnly;

    public bool IsLoggedIn;
    public bool IsAdmin;
    
    public bool ShouldShowList;
    public bool ShouldShowReasons;
    
    public BlockedInstance[] BlockedInstances = [];
    public AllowedInstance[] AllowedInstances = [];
    
    public async Task OnGet()
    {
        Request.Cookies.TryGetValue("sessions", out var sessions);
        if (sessions == null)
        {
            IsLoggedIn = false;
        }
        else
        {
            foreach (var session in sessions.Split("|"))
            {
                if (!(await db.Sessions.AnyAsync(p => p.Token == session))) continue;
                IsLoggedIn = true;
                Request.HttpContext.HideFooter();
                break;
            }
        }
        
        Request.Cookies.TryGetValue("admin_session", out var admSession);
        if ((await db.Sessions.AnyAsync(p => p.Token == admSession)))
            IsAdmin = true;
        
        IsBlocklist = security.Value.FederationMode == Enums.FederationMode.BlockList;
        ModeString  = IsBlocklist ? "Blocked" : "Allowed";
        
        ListRegOnly = security.Value.ExposeFederationList == Enums.ItemVisibility.Registered;
        ReasonsRegOnly = security.Value.ExposeBlockReasons == Enums.ItemVisibility.Registered;
        
        ShouldShowList = (security.Value.ExposeFederationList == Enums.ItemVisibility.Public) || (ListRegOnly && IsLoggedIn) || IsAdmin;
        ShouldShowReasons = (security.Value.ExposeBlockReasons == Enums.ItemVisibility.Public) || (ReasonsRegOnly && IsLoggedIn) || IsAdmin;

        if (IsBlocklist)
        {
            BlockedInstances = await db.BlockedInstances
                                       .Select(p => new BlockedInstance { Host = p.Host, Reason = p.Reason })
                                       .ToArrayAsync();
        }
        else
        {
            AllowedInstances = await db.AllowedInstances
                                       .Select(p => new AllowedInstance { Host = p.Host })
                                       .ToArrayAsync();
        }
    }
}
