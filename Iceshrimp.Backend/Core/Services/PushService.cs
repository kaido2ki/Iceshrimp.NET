using System.Net;
using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PushSubscription = Iceshrimp.Backend.Core.Database.Tables.PushSubscription;
using WebPushSubscription = Iceshrimp.WebPush.PushSubscription;

namespace Iceshrimp.Backend.Core.Services;

public class PushService(
	IEventService eventSvc,
	ILogger<PushService> logger,
	IServiceScopeFactory scopeFactory,
	HttpClient httpClient,
	IOptions<Config.InstanceSection> config
) : BackgroundService
{
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		eventSvc.Notification += MastodonPushHandler;
		//TODO: eventSvc.Notification += WebPushHandler;
		return Task.CompletedTask;
	}

	private async void MastodonPushHandler(object? _, Notification notification)
	{
		try
		{
			await using var scope = scopeFactory.CreateAsyncScope();
			await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

			var type = NotificationEntity.EncodeType(notification.Type);
			var subscriptions = await db.PushSubscriptions.Where(p => p.User == notification.Notifiee)
			                            .Include(pushSubscription => pushSubscription.OauthToken)
			                            .Where(p => p.Types.Contains(type))
			                            .ToListAsync();

			if (subscriptions.Count == 0)
				return;

			var skip = await db.Blockings.AnyAsync(p => p.Blocker == notification.Notifiee &&
			                                            p.Blockee == notification.Notifier) ||
			           await db.Mutings.AnyAsync(p => p.Muter == notification.Notifiee &&
			                                          p.Mutee == notification.Notifier);

			if (skip)
			{
				// Notifier is blocked or muted, so we shouldn't deliver the notification
				return;
			}

			logger.LogDebug("Delivering mastodon push notification {id} for user {userId}", notification.Id,
			                notification.Notifiee.Id);

			var isSelf = notification.Notifier == notification.Notifiee;

			var followed = subscriptions.All(p => p.Policy != PushSubscription.PushPolicy.Followed) ||
			               isSelf ||
			               await db.Followings.AnyAsync(p => p.Follower == notification.Notifiee &&
			                                                 p.Followee == notification.Notifier);

			var follower = subscriptions.All(p => p.Policy != PushSubscription.PushPolicy.Follower) ||
			               isSelf ||
			               await db.Followings.AnyAsync(p => p.Follower == notification.Notifier &&
			                                                 p.Followee == notification.Notifiee);

			var renderer = scope.ServiceProvider.GetRequiredService<NotificationRenderer>();
			var rendered = await renderer.RenderAsync(notification, notification.Notifiee);
			var name     = rendered.Notifier.DisplayName;
			var subject = rendered.Type switch
			{
				"favourite"      => $"{name} favorited your post",
				"follow"         => $"{name} is now following you",
				"follow_request" => $"Pending follower: {name}",
				"mention"        => $"You were mentioned by {name}",
				"poll"           => $"A poll by {name} has ended",
				"reblog"         => $"{name} boosted your post",
				"status"         => $"{name} just posted",
				"update"         => $"{name} edited a post",
				_                => $"New notification from {name}"
			};

			var body = "";

			if (notification.Note != null)
				body = notification.Note.Cw ?? notification.Note.Text ?? "";

			body = body.Trim().Truncate(140).TrimEnd();
			if (body.Length > 137)
				body = body.Truncate(137).TrimEnd() + "...";

			var meta = scope.ServiceProvider.GetRequiredService<MetaService>();
			var (priv, pub) = await meta.GetMany(MetaEntity.VapidPrivateKey, MetaEntity.VapidPublicKey);

			var client = new WebPushClient(httpClient);
			client.SetVapidDetails(new VapidDetails($"https://{config.Value.WebDomain}", pub, priv));

			var matchingSubscriptions =
				from subscription in subscriptions
				where subscription.Policy != PushSubscription.PushPolicy.Followed || followed
				where subscription.Policy != PushSubscription.PushPolicy.Follower || follower
				where subscription.Policy != PushSubscription.PushPolicy.None || isSelf
				select subscription;

			foreach (var subscription in matchingSubscriptions)
			{
				try
				{
					var res = new PushSchemas.PushNotification
					{
						AccessToken      = subscription.OauthToken.Token,
						NotificationType = rendered.Type,
						NotificationId   = long.Parse(rendered.Id),
						IconUrl          = rendered.Notifier.AvatarUrl,
						Title            = subject,
						Body             = body
					};

					var sub = new WebPushSubscription
					{
						Endpoint = subscription.Endpoint,
						P256DH   = subscription.PublicKey,
						Auth     = subscription.AuthSecret,
						PushMode = PushMode.AesGcm
					};

					await client.SendNotificationAsync(sub, JsonSerializer.Serialize(res));
				}
				catch (Exception e)
				{
					switch (e)
					{
						case WebPushException { StatusCode: HttpStatusCode.Gone }:
							await db.PushSubscriptions.Where(p => p.Id == subscription.Id).ExecuteDeleteAsync();
							break;
						case WebPushException we:
							logger.LogDebug("Push notification delivery failed: {e}", we.Message);
							break;
						default:
							logger.LogDebug("Push notification delivery threw exception: {e}", e);
							break;
					}
				}
			}
		}
		catch (GracefulException)
		{
			// Unsupported notification type
		}
		catch (Exception e)
		{
			logger.LogError("Event handler MastodonPushHandler threw exception: {e}", e);
		}
	}
}