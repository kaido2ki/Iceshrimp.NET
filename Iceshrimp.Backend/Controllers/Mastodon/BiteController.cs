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

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[Route("/api/v1/bite")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class BiteController(DatabaseContext db, BiteService biteSvc) : ControllerBase
{
    [HttpPost]
    [Authenticate("write:bites")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.BadRequest, HttpStatusCode.NotFound)]
    public async Task BiteUser([FromHybrid] string id)
    {
        var user = HttpContext.GetUserOrFail();
        if (user.Id == id)
            throw GracefulException.BadRequest("You cannot bite yourself");

        var target = await db.Bites.IncludeCommonProperties().FirstOrDefaultAsync(p => p.Id == id) ??
                     throw GracefulException.NotFound("User not found");

        await biteSvc.BiteAsync(user, target);
    }
}