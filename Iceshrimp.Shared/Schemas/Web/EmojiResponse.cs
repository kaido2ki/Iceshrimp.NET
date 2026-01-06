using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class EmojiResponse : IIdentifiable
{
    public required string  Id   { get; set; }
    public required string  Name { get; set; }
    public required string? Uri  { get; set; }

    /// <summary>
    /// List of alternative names
    /// </summary>
    public required List<string> Tags { get; set; }

    public required string? Category { get; set; }

    /// <summary>
    /// Image URL
    /// </summary>
    public required string PublicUrl { get; set; }

    /// <summary>
    /// Credits or licensing information
    /// </summary>
    public required string? License { get; set; }

    /// <summary>
    /// Whether the emoji should be blurred
    /// </summary>
    public required bool Sensitive { get; set; }
}
