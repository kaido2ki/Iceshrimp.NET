using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Mastodon.Renderers;

public class AnnouncementRenderer(
    DatabaseContext db,
    IOptions<Config.InstanceSection> config,
    MfmConverter mfmConverter
) : IScopedService
{
    private async Task<AnnouncementEntity> RenderAsync(
        Announcement announcement, User? user, AnnouncementRendererDto? data = null
    )
    {
        var mentions = data?.Mentions?.Where(p => announcement.Mentions.Contains(p.Id)).ToList()
                       ?? (await GetMentionsAsync([announcement])).Where(p => announcement.Mentions.Contains(p.Id))
                                                                  .ToList();

        var content = mfmConverter.ToHtml($"""
                                           **{announcement.Title}**
                                           {announcement.Text}
                                           """,
                                          mentions.Select(p => new Note.MentionedUser
                                                  {
                                                      Uri      = p.Uri,
                                                      Url      = p.Url,
                                                      Username = p.Username,
                                                      Host     = p.Host
                                                  })
                                                  .ToList(), null)
                                  .Html;

        var emojis = data?.Emojis?.Where(p => announcement.Emojis.Contains(p.Id)).ToList()
                     ?? await GetEmojisAsync([announcement]);

        var tags = data?.Tags?.Where(p => announcement.Tags.Contains(p.Name)).ToList()
                   ?? GetTags([announcement]).Where(p => announcement.Tags.Contains(p.Name)).ToList();

        var read = data?.Reads?.Contains(announcement.Id)
                   ?? (await GetReadsAsync([announcement], user)).Contains(announcement.Id);

        return new AnnouncementEntity
        {
            Id          = announcement.Id,
            Content     = content,
            PublishedAt = announcement.CreatedAt.ToStringIso8601Like(),
            UpdatedAt   = (announcement.UpdatedAt ?? announcement.CreatedAt).ToStringIso8601Like(),
            IsRead      = read,
            Mentions    = mentions,
            Emoji       = emojis,
            Tags        = tags
        };
    }

    public async Task<IEnumerable<AnnouncementEntity>> RenderManyAsync(
        IEnumerable<Announcement> announcements, User? user
    )
    {
        var announcementList = announcements.ToList();

        var data = new AnnouncementRendererDto
        {
            Mentions = await GetMentionsAsync(announcementList),
            Emojis   = await GetEmojisAsync(announcementList),
            Tags     = GetTags(announcementList),
            Reads    = await GetReadsAsync(announcementList, user)
        };

        return await announcementList.Select(p => RenderAsync(p, user, data)).AwaitAllAsync();
    }
    
    private async Task<List<MentionEntity>> GetMentionsAsync(List<Announcement> announcements)
    {
        if (announcements.Count == 0) return [];
        var ids = announcements.SelectMany(a => a.Mentions).Distinct();
        return await db.Users.IncludeCommonProperties()
                       .Where(p => ids.Contains(p.Id))
                       .Select(u => new MentionEntity(u, config.Value.WebDomain))
                       .ToListAsync();
    }

    private async Task<List<EmojiEntity>> GetEmojisAsync(IEnumerable<Announcement> announcements)
    {
        var ids = announcements.SelectMany(p => p.Emojis).ToList();
        if (ids.Count == 0) return [];

        return await db.Emojis
                       .Where(p => ids.Contains(p.Id))
                       .Select(p => new EmojiEntity
                       {
                           Id              = p.Id,
                           Shortcode       = p.Name.Trim(':'),
                           StaticUrl       = p.GetAccessUrl(config.Value),
                           Url             = p.GetAccessUrl(config.Value),
                           VisibleInPicker = true,
                           Category        = p.Category
                       })
                       .ToListAsync();
    }

    private List<StatusTags> GetTags(IEnumerable<Announcement> announcements)
    {
        var tags = announcements.SelectMany(p => p.Tags).ToList();
        if (tags.Count == 0) return [];

        return tags.Select(tag => new StatusTags
                   {
                       Name = tag,
                       Url =
                           $"https://{config.Value.WebDomain}/tags/{tag}"
                   })
                   .ToList();
    }
    
    private async Task<List<string>> GetReadsAsync(IEnumerable<Announcement> announcements, User? user)
    {
        if (user == null) return [];

        var ids = announcements.Select(p => p.Id).ToList();
        if (ids.Count == 0) return [];

        return await db.AnnouncementReads
                       .Where(p => p.UserId == user.Id && ids.Contains(p.AnnouncementId))
                       .Select(p => p.AnnouncementId)
                       .ToListAsync();
    }

    public class AnnouncementRendererDto
    {
        public List<MentionEntity>? Mentions;
        public List<EmojiEntity>?   Emojis;
        public List<StatusTags>?    Tags;
        public List<string>?        Reads;
    }
}
