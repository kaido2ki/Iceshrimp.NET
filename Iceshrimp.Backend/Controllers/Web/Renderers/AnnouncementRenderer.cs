using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Shared.Schemas.Web;
using Iceshrimp.Utils.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Controllers.Web.Renderers;

public class AnnouncementRenderer(
    DatabaseContext db,
    IOptions<Config.InstanceSection> config
) : IScopedService
{
    public async Task<AnnouncementResponse> RenderOneAsync(
        Announcement announcement, User? user, AnnouncementRendererDto? data = null
    )
    {
        var emojis = data?.Emojis?.Where(p => announcement.Emojis.Contains(p.Id)).ToList()
                     ?? await GetEmojisAsync([announcement]);

        var read = data?.Reads?.Contains(announcement.Id)
                   ?? (await GetReadsAsync([announcement], user)).Contains(announcement.Id);

        var readCount = data?.ReadCounts?.GetValueOrDefault(announcement.Id)
                        ?? (await GetReadCountsAsync([announcement], user)).GetValueOrDefault(announcement.Id);

        return new AnnouncementResponse
        {
            Id        = announcement.Id,
            CreatedAt = announcement.CreatedAt,
            UpdatedAt = announcement.UpdatedAt,
            Title     = announcement.Title,
            Text      = announcement.Text,
            Emojis    = emojis,
            ImageUrl  = announcement.ImageUrl,
            ShowPopup = announcement.ShowPopup,
            Read      = read,
            ReadCount = readCount
        };
    }

    public async Task<IEnumerable<AnnouncementResponse>> RenderManyAsync(
        IEnumerable<Announcement> announcements, User? user
    )
    {
        var announcementList = announcements.ToList();

        var data = new AnnouncementRendererDto
        {
            Emojis     = await GetEmojisAsync(announcementList),
            Reads      = await GetReadsAsync(announcementList, user),
            ReadCounts = await GetReadCountsAsync(announcementList, user)
        };

        return await announcementList.Select(p => RenderOneAsync(p, user, data)).AwaitAllAsync();
    }

    private async Task<List<EmojiResponse>> GetEmojisAsync(IEnumerable<Announcement> announcements)
    {
        var ids = announcements.SelectMany(p => p.Emojis).ToList();
        if (ids.Count == 0) return [];

        return await db.Emojis
                       .Where(p => ids.Contains(p.Id))
                       .Select(p => new EmojiResponse
                       {
                           Id        = p.Id,
                           Name      = p.Name,
                           Uri       = p.Uri,
                           Tags      = p.Tags,
                           Category  = p.Category,
                           PublicUrl = p.GetAccessUrl(config.Value),
                           License   = p.License,
                           Sensitive = p.Sensitive
                       })
                       .ToListAsync();
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

    private async Task<Dictionary<string, int>> GetReadCountsAsync(IEnumerable<Announcement> announcements, User? user)
    {
        if (user is null or { IsAdmin: false, IsModerator: false }) return [];

        var ids = announcements.Select(p => p.Id).ToList();
        if (ids.Count == 0) return [];

        var counts = await db.AnnouncementReads
                             .Where(p => ids.Contains(p.AnnouncementId))
                             .GroupBy(p => p.AnnouncementId)
                             .ToDictionaryAsync(p => p.Key, p => p.Count());

        var zeros = ids.Where(p => !counts.ContainsKey(p)).ToDictionary(p => p, _ => 0);

        return counts.Concat(zeros).ToDictionary();
    }

    public class AnnouncementRendererDto
    {
        public List<EmojiResponse>?      Emojis;
        public List<string>?             Reads;
        public Dictionary<string, int>?  ReadCounts;
    }
}
