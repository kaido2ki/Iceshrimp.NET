using System.Net;
using System.Net.Mime;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Web;

/// <summary>
/// Operations for managing notifications.
/// </summary>
[ApiController]
[Authenticate]
[Authorize]
[EnableRateLimiting("sliding")]
[Route("/api/iceshrimp/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[EnableCors("iceshrimp")]
public class NotificationController(DatabaseContext db, MetaService metaSvc, NotificationRenderer notificationRenderer)
	: ControllerBase
{
	/// <summary>
	/// List notifications
	/// </summary>
	/// <remarks>Returns a paginated list of notifications.</remarks>
	/// <param name="query">Pagination query</param>
	/// <response code="200">Paginated list of notifications</response>
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

		return await notificationRenderer.RenderManyAsync(notifications.EnforceRenoteReplyVisibility(p => p.Note),
		                                                  user);
	}

	/// <summary>
	/// Mark as read
	/// </summary>
	/// <param name="id">The notification's ID</param>
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

	/// <summary>
	/// Mark all as read
	/// </summary>
	[HttpPost("read")]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task MarkAllNotificationsAsRead()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user && !p.IsRead)
		        .ExecuteUpdateAsync(p => p.SetProperty(n => n.IsRead, true));
	}

	/// <summary>
	/// Remove notification
	/// </summary>
	/// <param name="id">The notification's ID</param>
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

	/// <summary>
	/// Remove all notifications
	/// </summary>
	[HttpDelete]
	[ProducesResults(HttpStatusCode.OK)]
	public async Task DeleteAllNotifications()
	{
		var user = HttpContext.GetUserOrFail();
		await db.Notifications.Where(p => p.Notifiee == user)
		        .ExecuteDeleteAsync();
	}

	/// <summary>
	/// Get push notification subscription
	/// </summary>
	/// <remarks>Get the push notifications subscription for the current session.</remarks>
	/// <returns>Web Push subscription response</returns>
	[HttpGet("push")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task<WebPushSubscriptionResponse> GetPushSubscription()
	{
		var session = HttpContext.GetSessionOrFail();

		var subscription = await db.SwSubscriptions.FirstOrDefaultAsync(p => p.SessionId == session.Id)
		                   ?? throw GracefulException.RecordNotFound();

		return new WebPushSubscriptionResponse
		{
			Id       = subscription.Id,
			Endpoint = subscription.Endpoint,
			VapidKey = await metaSvc.GetAsync(MetaEntity.VapidPublicKey)
		};
	}

	/// <summary>
	/// Subscribe to push notifications
	/// </summary>
	/// <remarks>Subscribe the current session to push notifications using the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Push_API">Web Push API</see>.</remarks>
	/// <param name="request">Web Push subscription request</param>
	/// <returns>Web Push subscription response</returns>
	[HttpPost("push")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.BadRequest)]
	public async Task<WebPushSubscriptionResponse> SubscribePush(WebPushSubscriptionRequest request)
	{
		var session = HttpContext.GetSessionOrFail();

		var subscription = await db.SwSubscriptions.FirstOrDefaultAsync(p => p.SessionId == session.Id);

		if (subscription == null)
		{
			if (!Uri.IsWellFormedUriString(request.Endpoint, UriKind.Absolute))
				throw GracefulException.BadRequest("Endpoint URL is malformed");

			subscription = new SwSubscription
			{
				Id         = IdHelpers.GenerateSnowflakeId(),
				CreatedAt  = DateTime.UtcNow,
				UserId     = session.UserId,
				SessionId  = session.Id,
				Endpoint   = request.Endpoint,
				PublicKey  = request.PublicKey,
				AuthSecret = request.AuthSecret
			};

			await db.AddAsync(subscription);
			await db.SaveChangesAsync();
		}

		return new WebPushSubscriptionResponse
		{
			Id       = subscription.Id,
			Endpoint = subscription.Endpoint,
			VapidKey = await metaSvc.GetAsync(MetaEntity.VapidPublicKey)
		};
	}

	/// <summary>
	/// Unsubscribe from push notifications
	/// </summary>
	/// <remarks>Unsubscribe the current session from push notifications.</remarks>
	[HttpDelete("push")]
	[ProducesResults(HttpStatusCode.OK)]
	[ProducesErrors(HttpStatusCode.NotFound)]
	public async Task UnsubscribePush()
	{
		var session = HttpContext.GetSessionOrFail();

		var subscription = await db.SwSubscriptions.FirstOrDefaultAsync(p => p.SessionId == session.Id)
		                   ?? throw GracefulException.RecordNotFound();

		db.Remove(subscription);
		await db.SaveChangesAsync();
	}
}