using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class AnnouncementResponse : IIdentifiable
{
    public required string              Id        { get; set; }
    public required DateTime            CreatedAt { get; set; }
    public required DateTime?           UpdatedAt { get; set; }
    public required string              Title     { get; set; }
    public required string              Text      { get; set; }
    public required List<EmojiResponse> Emojis    { get; set; }
    public required string?             ImageUrl  { get; set; }
    public required bool                ShowPopup { get; set; }
    public required bool                Read      { get; set; }
    public required int?                ReadCount { get; set; }
}
