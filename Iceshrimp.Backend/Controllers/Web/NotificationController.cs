using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/notifications")]
[Produces(MediaTypeNames.Application.Json)]
public class NotificationController(DatabaseContext db, NotificationRenderer notificationRenderer) : ControllerBase
{
	[HttpGet]
	[LinkPagination(20, 80)]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<IEnumerable<NotificationResponse>> GetNotifications(PaginationQuery query)
	{
		var user = HttpContext.GetUserOrFail();
		var notifications = await db.Notifications
		                            .Where(p => p.Notifiee == user)
		                            .IncludeCommonProperties()
		                            .EnsureNoteVisibilityFor(p => p.Note, user)
		                            .FilterHiddenNotifications(user, db)
		                            .Paginate(query, ControllerContext)
		                            .PrecomputeNoteVisibilities(user)
		                            .ToListAsync();

		return await notificationRenderer.RenderManyAsync(notifications.EnforceRenoteReplyVisibility(p => p.Note), user);
	}

	[HttpPost("{id}/read")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task MarkNotificationAsRead(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var notification = await db.Notifications.FirstOrDefaultAsync(p => p.Notifiee == user && p.Id == id) ??
		                   throw GracefulException.NotFound("Notification not found");

		if (!notification.IsRead)
		{
			notification.IsRead = true;
			await db.SaveChangesAsync();
		}
	}

	[HttpPost("read")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task MarkAllNotificationsAsRead()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user && !p.IsRead)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.IsRead, true));
	}

	[HttpDelete("{id}")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task DeleteNotification(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var notification = await db.Notifications.FirstOrDefaultAsync(p => p.Notifiee == user && p.Id == id) ??
		                   throw GracefulException.NotFound("Notification not found");

		db.Remove(notification);
		await db.SaveChangesAsync();
	}

	[HttpDelete]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task DeleteAllNotifications()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user)
		        .ExecuteDeleteAsync();
	}
}