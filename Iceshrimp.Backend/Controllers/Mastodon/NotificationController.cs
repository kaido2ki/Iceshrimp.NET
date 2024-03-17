using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Backend.Core.Database.Tables.Notification;

namespace Iceshrimp.Backend.Controllers.Mastodon;

[MastodonApiController]
[Route("/api/v1/notifications")]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class NotificationController(DatabaseContext db, NotificationRenderer notificationRenderer) : ControllerBase
{
	[HttpGet]
	[Authorize("read:notifications")]
	[LinkPagination(40, 80)]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<NotificationEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetNotifications(
		MastodonPaginationQuery query, NotificationSchemas.GetNotificationsRequest request
	)
	{
		var user = HttpContext.GetUserOrFail();
		var res = await db.Notifications
		                  .IncludeCommonProperties()
		                  .Where(p => p.Notifiee == user)
		                  .Where(p => p.Notifier != null)
		                  .Where(p => p.Type == NotificationType.Follow ||
		                              p.Type == NotificationType.Mention ||
		                              p.Type == NotificationType.Reply ||
		                              p.Type == NotificationType.Renote ||
		                              p.Type == NotificationType.Quote ||
		                              p.Type == NotificationType.Like ||
		                              p.Type == NotificationType.PollEnded ||
		                              p.Type == NotificationType.FollowRequestReceived ||
		                              p.Type == NotificationType.Edit)
		                  .FilterByGetNotificationsRequest(request)
		                  .EnsureNoteVisibilityFor(p => p.Note, user)
		                  .FilterBlocked(p => p.Notifier, user)
		                  .FilterBlocked(p => p.Note, user)
		                  .Paginate(query, ControllerContext)
		                  .PrecomputeNoteVisibilities(user)
		                  .RenderAllForMastodonAsync(notificationRenderer, user);

		return Ok(res);
	}

	[HttpGet("{id:long}")]
	[Authorize("read:notifications")]
	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<NotificationEntity>))]
	[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MastodonErrorResponse))]
	public async Task<IActionResult> GetNotification(long id)
	{
		var user = HttpContext.GetUserOrFail();
		var notification = await db.Notifications
		                           .IncludeCommonProperties()
		                           .Where(p => p.Notifiee == user && p.MastoId == id)
		                           .EnsureNoteVisibilityFor(p => p.Note, user)
		                           .PrecomputeNoteVisibilities(user)
		                           .FirstOrDefaultAsync() ??
		                   throw GracefulException.RecordNotFound();

		var res = await notificationRenderer.RenderAsync(notification.EnforceRenoteReplyVisibility(p => p.Note), user);

		return Ok(res);
	}
}