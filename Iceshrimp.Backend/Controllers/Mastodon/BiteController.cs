using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class BiteController(DatabaseContext db, BiteService biteSvc) : ControllerBase
{
    [HttpPost("/api/v1/bite")]
    [Authenticate("write:bites")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
    public async Task BiteUser([FromHybrid] string id)
    {
        var user = HttpContext.GetUserOrFail();
        if (user.Id == id)
            throw GracefulException.BadRequest("You cannot bite yourself");

        var target = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
                     throw GracefulException.NotFound("User not found");

        await biteSvc.BiteAsync(user, target);
    }
    
    [HttpPost("/api/v1/users/{id}/bite")]
    [Authenticate("write:bites")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
    public async Task BiteUser2(string id)
    {
        var user = HttpContext.GetUserOrFail();
        if (user.Id == id)
            throw GracefulException.BadRequest("You cannot bite yourself");

        var target = await db.Users.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
                     throw GracefulException.NotFound("User not found");

        await biteSvc.BiteAsync(user, target);
    }
    
    [HttpPost("/api/v1/users/{id}/bite_back")]
    [Authenticate("write:bites")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
    [SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataQuery")]
    [SuppressMessage("ReSharper", "EntityFramework.NPlusOne.IncompleteDataUsage")]
    public async Task BiteBack(string id)
    {
        var user = HttpContext.GetUserOrFail();
        var target = await db.Bites
                             .IncludeCommonProperties()
                             .Where(p => p.Id == id)
                             .FirstOrDefaultAsync() ??
                     throw GracefulException.NotFound("Bite not found");

        if (user.Id != (target.TargetUserId ?? target.TargetNote?.UserId ?? target.TargetBite?.UserId))
            throw GracefulException.BadRequest("You can only bite back at a user who bit you");

        await biteSvc.BiteAsync(user, target);
    }

    [HttpPost("/api/v1/statuses/{id}/bite")]
    [Authenticate("write:bites")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
    public async Task BiteStatus(string id)
    {
        var user = HttpContext.GetUserOrFail();
        if (user.Id == id)
            throw GracefulException.BadRequest("You cannot bite your own note");

        var target = await db.Notes
                             .Where(p => p.Id == id)
                             .IncludeCommonProperties()
                             .EnsureVisibleFor(user)
                             .FirstOrDefaultAsync() ??
                     throw GracefulException.NotFound("Note not found");

        await biteSvc.BiteAsync(user, target);
    }
}