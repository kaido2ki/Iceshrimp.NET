using System.Text.Json.Serialization;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
using static Iceshrimp.Backend.Core.Database.Tables.Notification;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class Notification : IEntity {
	[J("id")]         public required string  Id        { get; set; }
	[J("created_at")] public required string  CreatedAt { get; set; }
	[J("type")]       public required string  Type      { get; set; }
	[J("account")]    public required Account Notifier  { get; set; }
	[J("status")]     public required Status? Note      { get; set; }

	//TODO: [J("reaction")]     public required Reaction? Reaction      { get; set; }

	public static string EncodeType(NotificationType type) {
		return type switch {
			NotificationType.Follow                => "follow",
			NotificationType.Mention               => "mention",
			NotificationType.Reply                 => "mention",
			NotificationType.Renote                => "renote",
			NotificationType.Quote                 => "reblog",
			NotificationType.Reaction              => "favourite",
			NotificationType.PollEnded             => "poll",
			NotificationType.FollowRequestReceived => "follow_request",

			_ => throw new GracefulException($"Unsupported notification type: {type}")
		};
	}
}