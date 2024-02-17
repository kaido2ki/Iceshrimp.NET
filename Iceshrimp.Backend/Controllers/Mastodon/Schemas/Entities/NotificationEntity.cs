using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using static Iceshrimp.Backend.Core.Database.Tables.Notification;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class NotificationEntity : IEntity
{
	[J("created_at")] public required string        CreatedAt { get; set; }
	[J("type")]       public required string        Type      { get; set; }
	[J("account")]    public required AccountEntity Notifier  { get; set; }
	[J("status")]     public required StatusEntity? Note      { get; set; }
	[J("id")]         public required string        Id        { get; set; }

	//TODO: [J("reaction")]     public required Reaction? Reaction      { get; set; }

	public static string EncodeType(NotificationType type)
	{
		return type switch
		{
			NotificationType.Follow                => "follow",
			NotificationType.Mention               => "mention",
			NotificationType.Reply                 => "mention",
			NotificationType.Renote                => "renote",
			NotificationType.Quote                 => "reblog",
			NotificationType.Like                  => "favourite",
			NotificationType.PollEnded             => "poll",
			NotificationType.FollowRequestReceived => "follow_request",
			NotificationType.Edit                  => "update",

			_ => throw new GracefulException($"Unsupported notification type: {type}")
		};
	}
}