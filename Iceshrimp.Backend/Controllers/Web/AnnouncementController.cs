using System.Net;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[Route("/api/iceshrimp/announcements")]
public class AnnouncementController(DatabaseContext db, AnnouncementRenderer renderer) : ControllerBase
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
		var announcement = new Announcement
		{
			Id        = IdHelpers.GenerateSnowflakeId(),
			CreatedAt = DateTime.UtcNow,
			Title     = request.Title,
			Text      = request.Text,
			ImageUrl  = request.ImageUrl,
			ShowPopup = request.ShowPopup
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
		var announcement = await db.Announcements.FirstOrDefaultAsync(p => p.Id == id)
		                   ?? throw GracefulException.RecordNotFound();
		
		announcement.UpdatedAt = DateTime.UtcNow;
		announcement.Title     = request.Title;
		announcement.Text      = request.Text;
		announcement.ImageUrl  = request.ImageUrl;
		announcement.ShowPopup = request.ShowPopup;

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
}
