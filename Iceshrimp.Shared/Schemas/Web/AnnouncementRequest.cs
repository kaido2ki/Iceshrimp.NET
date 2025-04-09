namespace Iceshrimp.Shared.Schemas.Web;

public class AnnouncementRequest
{
    public required string  Title     { get; set; }
    public required string  Text      { get; set; }
    public required string? ImageUrl  { get; set; }
    public required bool    ShowPopup { get; set; }
}
