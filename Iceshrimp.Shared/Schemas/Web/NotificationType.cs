using System.Text.Json.Serialization;

namespace Iceshrimp.Shared.Schemas.Web;

// Keep in sync with Iceshrimp.Backend.Core.Database.Tables.Notification.NotificationType
public enum NotificationType
{
    Follow,
    Mention,
    Reply,
    Renote,
    Quote,
    Like,
    Reaction,
    PollVote,
    PollEnded,
    FollowRequestReceived,
    FollowRequestAccepted,
    GroupInvited,
    App,
    Edit,
    Bite,
    Report
}
