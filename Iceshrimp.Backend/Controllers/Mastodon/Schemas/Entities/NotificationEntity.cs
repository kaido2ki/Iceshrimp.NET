using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using static Iceshrimp.Backend.Core.Database.Tables.Notification;
using Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class NotificationEntity : IEntity
{
	[J("created_at")] public required string        CreatedAt { get; set; }
	[J("type")]       public required string        Type      { get; set; }
	[J("account")]    public required AccountEntity Notifier  { get; set; }
	[J("status")]     public required StatusEntity? Note      { get; set; }
	[J("id")]         public required string        Id        { get; set; }


	[J("emoji")]     public string? Emoji { get; set; }
	[J("emoji_url")] public string? EmojiUrl { get; set; }
	[J("pleroma")]   public required PleromaNotificationExtensions Pleroma { get; set; }

	public static string EncodeType(NotificationType type, bool isPleroma)
	{
		return type switch
		{
			NotificationType.Follow                => "follow",
			NotificationType.Mention               => "mention",
			NotificationType.Reply                 => "mention",
			NotificationType.Renote                => "reblog",
			NotificationType.Quote                 => "status",
			NotificationType.Like                  => "favourite",
			NotificationType.PollEnded             => "poll",
			NotificationType.FollowRequestReceived => "follow_request",
			NotificationType.Edit                  => "update",

			NotificationType.Reaction when isPleroma  => "pleroma:emoji_reaction",
			NotificationType.Reaction when !isPleroma => "reaction",

			_ => throw new GracefulException($"Unsupported notification type: {type}")
		};
	}

	public static IEnumerable<NotificationType> DecodeType(string type)
	{
		return type switch
		{
			"follow"         => [NotificationType.Follow],
			"mention"        => [NotificationType.Mention, NotificationType.Reply],
			"reblog"         => [NotificationType.Renote, NotificationType.Quote],
			"favourite"      => [NotificationType.Like],
			"poll"           => [NotificationType.PollEnded],
			"follow_request" => [NotificationType.FollowRequestReceived],
			"update"         => [NotificationType.Edit],
			"reaction"       => [NotificationType.Reaction],
			"pleroma:emoji_reaction" => [NotificationType.Reaction],
			_                => []
		};
	}
}