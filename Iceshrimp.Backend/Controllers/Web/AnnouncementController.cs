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
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/announcements")]
[EnableCors("iceshrimp")]
public class AnnouncementController(
	DatabaseContext db,
	AnnouncementRenderer renderer,
	ActivityPub.UserResolver userResolver,
	EmojiService emojiSvc
) : ControllerBase
{
	/// <summary>
	/// List announcements
	/// </summary>
	/// <remarks>Returns a paginated list of announcements.</remarks>
	/// <param name="popups">Only show unread popup announcements</param>
	/// <param name="pq">Pagination query</param>
	/// <response code="200">Paginated list of announcements</response>
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

	/// <summary>
	/// Create announcement
	/// </summary>
	/// <remarks>Create a new announcement.</remarks>
	/// <param name="request">Announcement request</param>
	/// <response code="200">Created announcement</response>
    [HttpPost]
    [Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<AnnouncementResponse> CreateAnnouncement(AnnouncementRequest request)
	{
		var parsedText = MfmParser.Parse(request.Title + " " + request.Text.ReplaceLineEndings("\n"));
		var mentions   = await GetMentionsAsync(parsedText);
		var emojis     = (await emojiSvc.ResolveEmojiAsync(parsedText)).Select(p => p.Id).ToList();
		var tags       = GetHashtags(parsedText);

		var announcement = new Announcement
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Title     = request.Title.Trim(),
			Text      = request.Text.Trim(),
			ImageUrl  = request.ImageUrl,
			ShowPopup = request.ShowPopup,
			Mentions  = mentions,
			Emojis    = emojis,
			Tags      = tags
		};

		db.Add(announcement);
		await db.SaveChangesAsync();

		return await renderer.RenderOneAsync(announcement, null);
	}

	/// <summary>
	/// Update announcement
	/// </summary>
	/// <remarks>Update the content of an announcement.</remarks>
	/// <param name="id">The announcement's ID</param>
	/// <param name="request">Announcement request</param>
	/// <response code="200">Updated announcement</response>
	[HttpPut("{id}")]
	[Authorize("role:moderator")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<AnnouncementResponse> UpdateAnnouncement(string id, AnnouncementRequest request)
	{
		var parsedText = MfmParser.Parse(request.Title + " " + request.Text.ReplaceLineEndings("\n"));
		var mentions   = await GetMentionsAsync(parsedText);
		var emojis     = (await emojiSvc.ResolveEmojiAsync(parsedText)).Select(p => p.Id).ToList();
		var tags       = GetHashtags(parsedText);

		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id)
		                   ?? throw GracefulException.RecordNotFound();

		announcement.UpdatedAt = DateTime.UtcNow;
		announcement.Title     = request.Title.Trim();
		announcement.Text      = request.Text.Trim();
		announcement.ImageUrl  = request.ImageUrl;
		announcement.ShowPopup = request.ShowPopup;
		announcement.Mentions  = mentions;
		announcement.Emojis    = emojis;
		announcement.Tags      = tags;

		db.Update(announcement);
		await db.SaveChangesAsync();

		return await renderer.RenderOneAsync(announcement, null);
	}

	/// <summary>
	/// Remove announcement
	/// </summary>
	/// <param name="id">The announcement's ID</param>
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

	/// <summary>
	/// Mark as read
	/// </summary>
	/// <remarks>Mark an announcement as read. Popup announcements that are marked as read won't pop up in the frontend.</remarks>
	/// <param name="id">The announcement's ID</param>
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

	private async Task<List<string>> GetMentionsAsync(IMfmNode[] nodes)
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

		return users.NotNull().Select(p => p.Id).Distinct().ToList();
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
