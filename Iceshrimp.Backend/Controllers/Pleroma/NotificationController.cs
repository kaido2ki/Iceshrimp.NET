using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Mastodon.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Pleroma;

[MastodonApiController]
[Authenticate]
[EnableCors("mastodon")]
[EnableRateLimiting("sliding")]
[Produces(MediaTypeNames.Application.Json)]
public class NotificationController(DatabaseContext db, NotificationRenderer notificationRenderer) : ControllerBase
{
	[HttpPost("/api/v1/pleroma/notifications/read")]
	[Authorize("read:notifications")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task<List<NotificationEntity>> MarkNotificationsAsRead([FromHybrid] PleromaNotificationSchemas.ReadNotificationsRequest request)
	{
		var user = HttpContext.GetUserOrFail();

		if (request.Id != null && request.MaxId != null)
			throw GracefulException.BadRequest("id and max_id are mutually exclusive.");

		var q = db.Notifications
			.IncludeCommonProperties()
			.Include(p => p.Notifier)
			.Include(p => p.Note)
			.Where(p => p.Notifiee == user)
			.Where(p => p.Notifier != null)
			.Where(p => !p.IsRead)
			.EnsureNoteVisibilityFor(p => p.Note, user)
			.OrderByDescending(n => n.MastoId)
			.Take(80);

		if (request.Id != null)
			q = q.Where(n => n.MastoId == request.Id);
		else if (request.MaxId != null)
			q = q.Where(n => n.MastoId <= request.MaxId);
		else
			throw GracefulException.BadRequest("One of id or max_id are required.");

		var notifications = await q.ToListAsync();
		foreach (var notif in notifications)
			notif.IsRead = true;

		await db.SaveChangesAsync();
		return (await notificationRenderer.RenderManyAsync(notifications, user, isPleroma: true)).ToList();
	}
}
