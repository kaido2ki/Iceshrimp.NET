using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Utils.DependencyInjection; 
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/scheduled_statuses")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class ScheduledStatusController(DatabaseContext db, NoteRenderer noteRenderer, NoteService noteService) : ControllerBase, IScopedService
{
    [Authenticate("read:statuses")]
    [LinkPagination(20, 40)]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<List<ScheduledStatusEntity>> GetScheduledNotes(MastodonPaginationQuery query)
    {
        var user = HttpContext.GetUserOrFail();

        return await db.Notes
                           .IncludeUnpublished()
                           .IncludeCommonProperties()
                           .FilterByUser(user)
                           .Where(p => p.ScheduledAt != null)
                           .Paginate(query, ControllerContext)
                           .RenderAllScheduledForMastodonAsync(noteRenderer, user);
    }
    
    [HttpGet("{id}")]
    [Authenticate("read:statuses")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<ScheduledStatusEntity> GetScheduledNote(string id)
    {
        var user = HttpContext.GetUserOrFail();

        var note = await db.Notes
                           .IncludeUnpublished()
                           .Where(p => p.Id == id && p.User == user && p.ScheduledAt != null)
                           .IncludeCommonProperties()
                           .FirstOrDefaultAsync() ??
                   throw GracefulException.RecordNotFound();

        return await noteRenderer.RenderScheduledAsync(note.EnforceRenoteReplyVisibility(), user);
    }
    
    [HttpPut("{id}")]
    [Authenticate("write:statuses")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<ScheduledStatusEntity> RescheduleScheduledNote(string id, [FromHybrid] StatusSchemas.RescheduleRequest request)
    {
        if (request.ScheduledAt.ToUniversalTime() < DateTime.UtcNow.AddMinutes(1))
            throw GracefulException.UnprocessableEntity("Scheduled note can not be in the past");

        var user = HttpContext.GetUserOrFail();

        var note = await db.Notes
                           .IncludeUnpublished()
                           .Where(p => p.Id == id && p.User == user && p.ScheduledAt != null)
                           .IncludeCommonProperties()
                           .FirstOrDefaultAsync() ??
                   throw GracefulException.RecordNotFound();

        await noteService.RescheduleNoteAsync(note, request.ScheduledAt);
        return await noteRenderer.RenderScheduledAsync(note.EnforceRenoteReplyVisibility(), user);
    }
    
    [HttpDelete("{id}")]
    [Authenticate("write:statuses")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<EmptyObject> DeleteScheduledNote(string id)
    {
        var user = HttpContext.GetUserOrFail();

        var note = await db.Notes
                           .IncludeUnpublished()
                           .Where(p => p.Id == id && p.User == user && p.ScheduledAt != null)
                           .IncludeCommonProperties()
                           .FirstOrDefaultAsync();

       if (note != null) await noteService.DeleteScheduledNoteAsync(note);
       return new EmptyObject();
    }

    public class EmptyObject;
}
