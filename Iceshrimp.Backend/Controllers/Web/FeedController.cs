using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[EnableRateLimiting("sliding")]
[Route("/users/{id}")]
public class FeedController(DatabaseContext db, IOptions<Config.InstanceSection> config, MfmConverter mfmConverter)
    : ControllerBase
{
    [HttpGet("feed.atom")]
    [LinkPagination(20, 80)]
    [Produces("application/atom+xml")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<AtomFeed> GetAtomFeed(string id, PaginationQuery pq)
    {
        var target = await db.Users.Include(p => p.UserSettings).FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser)
                     ?? throw GracefulException.RecordNotFound();

        // If user is in private mode don't generate an Atom feed
        if (target.UserSettings?.PrivateMode ?? true)
            throw GracefulException.Forbidden("Can't view Atom feed for private users");

        var notes = await db.Notes
                            .IncludeCommonProperties()
                            .FilterByUser(target)
                            .Where(p => p.Visibility == Note.NoteVisibility.Public)
                            .Paginate(pq, ControllerContext)
                            .ToListAsync();

        var newestNote = await db.Notes.FilterByUser(target)
                                 .Where(p => p.Visibility == Note.NoteVisibility.Public)
                                 .FirstOrDefaultAsync();

        var entries = notes.Select(p => new AtomEntry
                           {
                               Content = new AtomInlineTextContent
                               {
                                   Type = "html",
                                   Text =
                                       mfmConverter
                                           .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost)
                                           .Html
                               },
                               Id = new AtomId { Uri = p.GetPublicUri(config.Value) },
                               Links =
                               [
                                   new AtomLink
                                   {
                                       Href = p.GetPublicUri(config.Value),
                                       Rel  = "alternate",
                                       Type = "text/html"
                                   }
                               ],
                               PublishedAt = p.CreatedAt,
                               Title = new AtomPlainText { Text = $"Note by {target.DisplayName ?? target.Username}" },
                               UpdatedAt = p.UpdatedAt ?? p.CreatedAt
                           })
                           .ToList();

        List<AtomLink> links =
        [
            new AtomLink
            {
                Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom",
                Rel  = "self",
                Type = "application/atom+xml"
            },
            new AtomLink
            {
                Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom?max_id={notes.Last().Id}",
                Rel  = "next",
                Type = "application/atom+xml"
            }
        ];
        if (pq.MaxId != null)
        {
            links.Add(new AtomLink
            {
                Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom?min_id={notes.First().Id}",
                Rel  = "prev",
                Type = "application/atom+xml"
            });
        }

        return new AtomFeed
        {
            Authors =
            [
                new AtomPerson
                {
                    Name = target.DisplayName ?? target.Username, Uri = target.GetUriOrPublicUri(config.Value)
                }
            ],
            Generator =
                new AtomGenerator
                {
                    Text    = "Iceshrimp.NET",
                    Uri     = "https://iceshrimp.dev/iceshrimp/Icesrhimp.NET",
                    Version = VersionHelpers.VersionInfo.Value.Version
                },
            Icon      = new AtomIcon { Uri = target.GetAvatarUrl(config.Value) },
            Id        = new AtomId { Uri   = $"https://{config.Value.WebDomain}/users/{id}/feed.atom" },
            Links     = links,
            Title     = new AtomPlainText { Text = $"Notes by {target.DisplayName ?? target.Username}" },
            UpdatedAt = newestNote?.UpdatedAt ?? newestNote?.CreatedAt ?? target.CreatedAt,
            Entries   = entries
        };
    }
}
