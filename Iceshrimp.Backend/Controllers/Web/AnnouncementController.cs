using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.MfmSharp;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[Route("/api/iceshrimp/announcements")]
public class AnnouncementController(
	DatabaseContext db,
	AnnouncementRenderer renderer,
	ActivityPub.UserResolver userResolver,
	EmojiService emojiSvc
) : ControllerBase
{
	[HttpGet]
	[RestPagination(20, 40)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<PaginationWrapper<List<AnnouncementResponse>>> GetAnnouncements([FromQuery] bool popups, PaginationQuery pq)
	{
		var user = HttpContext.GetUserOrFail();

		var announcements = await db.Announcements
		                            .Include(p => p.AnnouncementReads)
		                            .Where(p => !popups || (p.ShowPopup && !p.ReadBy.Contains(user)))
		                            .Paginate(pq, ControllerContext)
		                            .ToListAsync();

		return HttpContext.CreatePaginationWrapper(pq, (await renderer.RenderManyAsync(announcements, user)).ToList());
	}

    [HttpPost]
    [Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<AnnouncementResponse> CreateAnnouncement(AnnouncementRequest request)
	{
		var parsedText = MfmParser.Parse(request.Title + " " + request.Text.ReplaceLineEndings("\n"));
		var (mentions, remote) = await GetMentionsAsync(parsedText);
		var emojis = (await emojiSvc.ResolveEmojiAsync(parsedText)).Select(p => p.Id).ToList();
		var tags   = GetHashtags(parsedText);

		var announcement = new Announcement
		{
			Id                   = IdHelpers.GenerateSnowflakeId(),
			CreatedAt            = DateTime.UtcNow,
			Title                = request.Title.Trim(),
			Text                 = request.Text.Trim(),
			ImageUrl             = request.ImageUrl,
			ShowPopup            = request.ShowPopup,
			Mentions             = mentions,
			MentionedRemoteUsers = remote,
			Emojis               = emojis,
			Tags                 = tags
		};

		db.Add(announcement);
		await db.SaveChangesAsync();

		return await renderer.RenderOneAsync(announcement, null);
	}

	[HttpPut("{id}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<AnnouncementResponse> UpdateAnnouncement(string id, AnnouncementRequest request)
	{
		var parsedText = MfmParser.Parse(request.Title + " " + request.Text.ReplaceLineEndings("\n"));
		var (mentions, remote) = await GetMentionsAsync(parsedText);
		var emojis = (await emojiSvc.ResolveEmojiAsync(parsedText)).Select(p => p.Id).ToList();
		var tags   = GetHashtags(parsedText);

		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id)
		                   ?? throw GracefulException.RecordNotFound();

		announcement.UpdatedAt            = DateTime.UtcNow;
		announcement.Title                = request.Title.Trim();
		announcement.Text                 = request.Text.Trim();
		announcement.ImageUrl             = request.ImageUrl;
		announcement.ShowPopup            = request.ShowPopup;
		announcement.Mentions             = mentions;
		announcement.MentionedRemoteUsers = remote;
		announcement.Emojis               = emojis;
		announcement.Tags                 = tags;

		db.Update(announcement);
		await db.SaveChangesAsync();

		return await renderer.RenderOneAsync(announcement, null);
	}

	[HttpDelete("{id}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteAnnouncement(string id)
	{
		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id)
		                   ?? throw GracefulException.RecordNotFound();

		db.Remove(announcement);
		await db.SaveChangesAsync();
	}

	[HttpPost("{id}/read")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task ReadAnnouncement(string id)
	{
		var user = HttpContext.GetUserOrFail();

		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id)
		                   ?? throw GracefulException.RecordNotFound();

		var existing = await db.AnnouncementReads.AnyAsync(p => p.AnnouncementId == id && p.UserId == user.Id);
		if (existing) return;

		var read = new AnnouncementRead
		{
			Id           = IdHelpers.GenerateSnowflakeId(),
			Announcement = announcement,
			User         = user,
			CreatedAt    = DateTime.UtcNow
		};

		db.Add(read);
		await db.SaveChangesAsync();
		await db.ReloadEntityRecursivelyAsync(read);
	}

	private async Task<(List<string>, List<Note.MentionedUser>)> GetMentionsAsync(IMfmNode[] nodes)
	{
		var mentions = nodes
		               .SelectMany(p => p.Children.Append(p))
		               .OfType<MfmMentionNode>()
		               .DistinctBy(p => p.Acct)
		               .ToArray();

		if (mentions.Length > 100)
			throw GracefulException.UnprocessableEntity("Refusing to process note with more than 100 mentions");

		var users = await mentions.Select(p => userResolver.ResolveOrNullAsync($"acct:{p.Acct}", ActivityPub.UserResolver.ResolveFlags.Acct))
		                          .AwaitAllNoConcurrencyAsync();
		
		var remoteMentions = users.NotNull()
		                          .Where(p => p is { IsRemoteUser: true, Uri: not null })
		                          .Select(p => new Note.MentionedUser
		                          {
			                          Host     = p.Host!,
			                          Uri      = p.Uri!,
			                          Username = p.Username,
			                          Url      = p.UserProfile?.Url
		                          })
		                          .ToList();

		return (users.NotNull().Select(p => p.Id).Distinct().ToList(), remoteMentions);
	}

	private List<string> GetHashtags(IMfmNode[] nodes)
	{
		return nodes.SelectMany(p => p.Children.Append(p))
		            .OfType<MfmHashtagNode>()
		            .Select(p => p.Hashtag.ToLowerInvariant())
		            .Select(p => p.Trim('#'))
		            .Distinct()
		            .ToList();
	}
}
