using System.Net;
using System.Net.Mime;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
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
    private static readonly XmlSerializerNamespaces XmlNamespaces  = new([new XmlQualifiedName("", "")]);
    private static readonly XmlSerializer           AtomSerializer = new(typeof(AtomFeed));
    private static readonly XmlSerializer           RssSerializer  = new(typeof(RssFeed));

    [HttpGet("feed.atom")]
    [LinkPagination(20, 80)]
    [Produces("application/atom+xml")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<ContentResult> GetAtomFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

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

        var feed = new AtomFeed
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

        // AtomSerializer is used to ensure that no unnecessary namespaces are added and the XML declaration is present
        using var stream = new MemoryStream();
        AtomSerializer.Serialize(stream, feed, XmlNamespaces);
        return Content(Encoding.UTF8.GetString(stream.ToArray()));
    }

    [HttpGet("feed.json")]
    [LinkPagination(20, 80)]
    [Produces("application/feed+json", MediaTypeNames.Application.Json)]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<JsonFeed> GetJsonFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

        var fileIds = notes.SelectMany(p => p.FileIds).Distinct().ToList();

        var files = await db.DriveFiles.Where(p => fileIds.Contains(p.Id))
                            .ToDictionaryAsync(p => p.Id,
                                               p => new JsonFeedAttachment
                                               {
                                                   Url       = p.RawAccessUrl,
                                                   MimeType  = p.PublicMimeType ?? p.Type,
                                                   FileName  = p.Name,
                                                   SizeBytes = p.Size
                                               });

        var items = notes.Select(p => new JsonFeedItem
                         {
                             Id  = p.GetPublicUri(config.Value),
                             Url = p.GetPublicUri(config.Value),
                             ContentHtml = mfmConverter
                                           .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost)
                                           .Html,
                             CreatedAt = p.CreatedAt,
                             UpdatedAt = p.UpdatedAt,
                             Attachments = p.FileIds.Count != 0
                                 ? p.FileIds.Select(i => files[i]).NotNull().ToList()
                                 : null
                         })
                         .ToList();

        var targetUrl = target.GetUriOrPublicUri(config.Value);
        var iconUrl   = target.GetAvatarUrl(config.Value);

        return new JsonFeed
        {
            Title       = $"Notes by {target.DisplayName ?? target.Username}",
            HomePageUrl = targetUrl,
            Uri         = $"https://{config.Value.WebDomain}/users/{id}/feed.json",
            NextUrl     = $"https://{config.Value.WebDomain}/users/{id}/feed.json?max_id={notes.Last().Id}",
            IconUrl     = iconUrl,
            FaviconUrl  = iconUrl,
            Authors =
            [
                new JsonFeedAuthor
                {
                    Name = target.DisplayName ?? target.Username, AvatarUrl = iconUrl, Url = targetUrl
                }
            ],
            Items = items
        };
    }

    [HttpGet("feed.rss")]
    [LinkPagination(20, 80)]
    [Produces("application/rss+xml")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<ContentResult> GetRssFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

        var fileIds = notes.SelectMany(p => p.FileIds).Distinct().ToList();

        var enclosures = await db.DriveFiles.Where(p => fileIds.Contains(p.Id))
                                 .ToDictionaryAsync(p => p.Id,
                                                    p => new RssEnclosure
                                                    {
                                                        Url  = p.RawAccessUrl,
                                                        Size = p.Size,
                                                        Type = p.PublicMimeType ?? p.Type
                                                    });

        var items = notes.Select(p => new RssItem
                         {
                             Title = $"Note by {target.DisplayName ?? target.Username}",
                             Link  = p.GetPublicUri(config.Value),
                             Description = mfmConverter
                                           .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost)
                                           .Html,
                             Enclosures = p.FileIds.Select(i => enclosures[i]).NotNull().ToList(),
                             Guid       = new RssGuid { IsPermaLink = true, Guid = p.GetPublicUri(config.Value) },
                             CreatedAt  = p.CreatedAt.ToString("r")
                         })
                         .ToList();

        var feed = new RssFeed
        {
            Channel = new RssChannel
            {
                Title       = $"Notes by {target.DisplayName ?? target.Username}",
                Link        = target.GetUriOrPublicUri(config.Value),
                Description = $"Public notes by {target.DisplayName ?? target.Username}",
                Generator   = $"Iceshrimp.NET {VersionHelpers.VersionInfo.Value.Version}",
                Items       = items
            }
        };

        // RssSerializer is used to ensure that no unnecessary namespaces are added and the XML declaration is present
        using var stream = new MemoryStream();
        RssSerializer.Serialize(stream, feed, XmlNamespaces);
        return Content(Encoding.UTF8.GetString(stream.ToArray()));
    }

    private async Task<(User, List<Note>)> GetTargetAndNotes(string id, PaginationQuery pq)
    {
        var target =
            await db.Users.Include(p => p.UserProfile)
                    .Include(p => p.UserSettings)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser)
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

        return (target, notes);
    }
}
