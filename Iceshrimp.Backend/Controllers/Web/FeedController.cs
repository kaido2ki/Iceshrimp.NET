using System.Net;
using System.Net.Mime;
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
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web;

#if DEBUG
[ApiExplorerSettings(IgnoreApi = false)]
#else
[ApiExplorerSettings(IgnoreApi = true)]
#endif
[ApiController]
[EnableRateLimiting("sliding")]
[Route("/users/{id}")]
public class FeedController(
	DatabaseContext db,
	IOptions<Config.InstanceSection> config,
	MfmConverter mfmConverter,
	FlagService flags
) : ControllerBase
{
    private static readonly XmlSerializerNamespaces XmlNamespaces  = new([new XmlQualifiedName("", "")]);
    private static readonly XmlSerializer           AtomSerializer = new(typeof(AtomFeed));
    private static readonly XmlSerializer           RssSerializer  = new(typeof(RssFeed));

    [HttpGet("feed.atom")]
    [LinkPagination(20, 80)]
    [Produces("application/atom+xml")]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<FileStreamResult> GetAtomFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

        // Make sure we don't lose inline HTML markup for outgoing federation
        flags.SupportsHtmlFormatting.Value = true;

        var entries = await notes.Select(p => new AtomEntry
                                 {
                                     RawId = p.Id,
                                     Content = new AtomInlineTextContent
                                     {
                                         Type = "html",
                                         Text =
                                             mfmConverter
                                                 .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost, null, false,
                                                         false,
                                                         "div", null, null)
                                                 .Html
                                     },
                                     Id = new AtomId { Uri = p.GetPublicUri(config.Value) },
                                     Links = new List<AtomLink>
                                     {
                                         new AtomLink
                                         {
                                             Href = p.GetPublicUri(config.Value),
                                             Rel  = "alternate",
                                             Type = "text/html"
                                         }
                                     },
                                     PublishedAt = p.CreatedAt,
                                     Title = new AtomPlainText
                                     {
                                         Text = $"Note by {target.DisplayName ?? target.Username}"
                                     },
                                     UpdatedAt = p.UpdatedAt ?? p.CreatedAt
                                 })
                                 .ToListAsync();

        List<AtomLink> links =
        [
            new AtomLink
            {
                Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom",
                Rel  = "self",
                Type = "application/atom+xml"
            }
        ];
        if (entries.Count > 0)
        {
            links.Add(new AtomLink
              {
                  Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom?max_id={entries.Last().RawId}",
                  Rel  = "next",
                  Type = "application/atom+xml"
              });
        }
        if (pq.MaxId != null)
        {
            links.Add(new AtomLink
            {
                Href = $"https://{config.Value.WebDomain}/users/{id}/feed.atom?min_id={entries.First().RawId}",
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
                    Uri     = Constants.ProjectHomepageUrl,
                    Version = VersionHelpers.VersionInfo.Value.Version
                },
            Icon      = new AtomIcon { Uri = target.GetAvatarUrl(config.Value) },
            Id        = new AtomId { Uri   = $"https://{config.Value.WebDomain}/users/{id}/feed.atom" },
            Links     = links,
            Title     = new AtomPlainText { Text = $"Notes by {target.DisplayName ?? target.Username}" },
            UpdatedAt = target.LastActiveDate ?? target.CreatedAt,
            Entries   = entries
        };

        // AtomSerializer is used to ensure that no unnecessary namespaces are added and the XML declaration is present
        var stream = new MemoryStream();
        AtomSerializer.Serialize(stream, feed, XmlNamespaces);
        stream.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(stream, "application/atom+xml");
    }

    [HttpGet("feed.json")]
    [LinkPagination(20, 80)]
    [Produces("application/feed+json", MediaTypeNames.Application.Json)]
    [ProducesResults(HttpStatusCode.OK)]
    [ProducesErrors(HttpStatusCode.Forbidden, HttpStatusCode.NotFound)]
    public async Task<JsonFeed> GetJsonFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

        // Make sure we don't lose inline HTML markup for outgoing federation
        flags.SupportsHtmlFormatting.Value = true;

        var items = await notes.Select(p => new JsonFeedItem
                               {
                                   RawId = p.Id,
                                   Id    = p.GetPublicUri(config.Value),
                                   Url   = p.GetPublicUri(config.Value),
                                   ContentHtml = mfmConverter
                                                 .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost, null, false,
                                                         false,
                                                         "div", null, null)
                                                 .Html,
                                   CreatedAt = p.CreatedAt,
                                   UpdatedAt = p.UpdatedAt,
                                   Attachments = db.DriveFiles.Where(f => p.FileIds.Contains(f.Id))
                                                   .Select(f => new JsonFeedAttachment
                                                   {
                                                       Url       = f.RawAccessUrl,
                                                       MimeType  = f.PublicMimeType ?? f.Type,
                                                       FileName  = p.Name,
                                                       SizeBytes = f.Size
                                                   })
                                                   .ToList()
                               })
                               .ToListAsync();

        var targetUrl = target.GetUriOrPublicUri(config.Value);
        var iconUrl   = target.GetAvatarUrl(config.Value);

        var nextUrl = items.Count > 0
            ? $"https://{config.Value.WebDomain}/users/{id}/feed.json?max_id={items.Last().RawId}"
            : null;

        return new JsonFeed
        {
            Title       = $"Notes by {target.DisplayName ?? target.Username}",
            HomePageUrl = targetUrl,
            Uri         = $"https://{config.Value.WebDomain}/users/{id}/feed.json",
            NextUrl     = nextUrl,
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
    public async Task<FileStreamResult> GetRssFeed(string id, PaginationQuery pq)
    {
        var (target, notes) = await GetTargetAndNotes(id, pq);

        // Make sure we don't lose inline HTML markup for outgoing federation
        flags.SupportsHtmlFormatting.Value = true;

        var items = await notes.Select(p => new RssItem
                               {
                                   Title = $"Note by {target.DisplayName ?? target.Username}",
                                   Link  = p.GetPublicUri(config.Value),
                                   Description = mfmConverter
                                                 .ToHtml(p.Text ?? "", p.MentionedRemoteUsers, p.UserHost, null, false,
                                                         false,
                                                         "div", null, null)
                                                 .Html,
                                   Enclosures =
                                       db.DriveFiles.Where(f => p.FileIds.Contains(f.Id))
                                         .Select(f => new RssEnclosure
                                         {
                                             Url  = f.RawAccessUrl,
                                             Size = f.Size,
                                             Type = f.PublicMimeType ?? f.Type
                                         })
                                         .ToList(),
                                   Guid      = new RssGuid { IsPermaLink = true, Guid = p.GetPublicUri(config.Value) },
                                   CreatedAt = p.CreatedAt.ToString("r")
                               })
                               .ToListAsync();

        var feed = new RssFeed
        {
            Channel = new RssChannel
            {
                Title       = $"Notes by {target.DisplayName ?? target.Username}",
                Link        = target.GetUriOrPublicUri(config.Value),
                Description = $"Public notes by {target.DisplayName ?? target.Username}",
                PublishedAt = (target.LastActiveDate ?? target.CreatedAt).ToString("r"),
                Generator   = $"Iceshrimp.NET {VersionHelpers.VersionInfo.Value.Version}",
                Items       = items
            }
        };

        // RssSerializer is used to ensure that no unnecessary namespaces are added and the XML declaration is present
        var stream = new MemoryStream();
        RssSerializer.Serialize(stream, feed, XmlNamespaces);
        stream.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(stream, "application/rss+xml");
    }

    private async Task<(User, IQueryable<Note>)> GetTargetAndNotes(string id, PaginationQuery pq)
    {
        var target =
            await db.Users.Include(p => p.UserProfile)
                    .Include(p => p.UserSettings)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsLocalUser)
            ?? throw GracefulException.RecordNotFound();

        // If user is in private mode don't generate an Atom feed
        if (target.UserSettings?.PrivateMode ?? true)
            throw GracefulException.Forbidden("Can't view Atom feed for private users");

        var notes = db.Notes
                      .IncludeCommonProperties()
                      .Where(p => !p.IsPureRenote)
                      .FilterByUser(target)
                      .Where(p => p.Visibility == Note.NoteVisibility.Public)
                      .Paginate(pq, ControllerContext);

        return (target, notes);
    }
}
