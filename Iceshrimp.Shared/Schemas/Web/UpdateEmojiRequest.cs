namespace Iceshrimp.Shared.Schemas.Web;

public class UpdateEmojiRequest
{
    public string? Name { get; set; }

    /// <summary>
    /// List of alternative names
    /// </summary>
    public List<string>? Tags { get; set; }

    public string? Category { get; set; }

    /// <summary>
    /// Credits or licensing information
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Whether the emoji should be blurred
    /// </summary>
    public bool? Sensitive { get; set; }
}

public class BatchUpdateEmojiRequest
{
    public required List<string> Ids      { get; set; }
    public          string?      Category { get; set; }

    /// <summary>
    /// Credits or licensing information
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Whether the emojis should be blurred
    /// </summary>
    public bool? Sensitive { get; set; }
}
