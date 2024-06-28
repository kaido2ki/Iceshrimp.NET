using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers;

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
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<NotificationResponse>))]
	public async Task<IActionResult> GetNotifications(PaginationQuery query)
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

		return Ok(await notificationRenderer.RenderMany(notifications.EnforceRenoteReplyVisibility(p => p.Note), user));
	}

	[HttpPost("{id}/read")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> MarkNotificationAsRead(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var notification = await db.Notifications.FirstOrDefaultAsync(p => p.Notifiee == user && p.Id == id) ??
		                   throw GracefulException.NotFound("Notification not found");

		if (!notification.IsRead)
		{
			notification.IsRead = true;
			await db.SaveChangesAsync();
		}

		return Ok(new object());
	}

	[HttpPost("read")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	public async Task<IActionResult> MarkAllNotificationsAsRead()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user && !p.IsRead)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.IsRead, true));

		return Ok(new object());
	}

	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
	public async Task<IActionResult> DeleteNotification(string id)
	{
		var user = HttpContext.GetUserOrFail();
		var notification = await db.Notifications.FirstOrDefaultAsync(p => p.Notifiee == user && p.Id == id) ??
		                   throw GracefulException.NotFound("Notification not found");

		db.Remove(notification);
		await db.SaveChangesAsync();

		return Ok(new object());
	}

	[HttpDelete]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
	public async Task<IActionResult> DeleteAllNotifications()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user)
		        .ExecuteDeleteAsync();

		return Ok(new object());
	}
}