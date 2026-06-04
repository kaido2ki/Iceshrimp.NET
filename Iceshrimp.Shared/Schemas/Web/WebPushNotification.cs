namespace Iceshrimp.Shared.Schemas.Web;

public class WebPushNotification
{
    public required string  Id               { get; set; }
    public required string  NotifieeId       { get; set; }
    public required string  NotifieeName     { get; set; }
    public required string  InstanceName     { get; set; }
    public required string  Type             { get; set; }
    public required string? IconUrl          { get; set; }
    public required string? NotifierId       { get; set; }
    public required string? NotifierName     { get; set; }
    public required string? NotifierUsername { get; set; }
    public required string? NoteId           { get; set; }
    public required string? Reaction         { get; set; }
    public required string? ReportId         { get; set; }

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
}
