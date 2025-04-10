namespace Iceshrimp.Shared.Schemas.Web;

public class StatusResponse
{
    public required bool UnreadAnnouncements { get; set; }
    public required bool UnreadNotifications { get; set; }
}
