namespace Iceshrimp.Shared.Schemas.Web;

public class UpdateDriveFileRequest
{
    public string? Filename { get; set; }

    /// <summary>
    /// Whether the file should be blurred
    /// </summary>
    public bool? Sensitive { get; set; }

    /// <summary>
    /// File alt text
    /// </summary>
    public string? Description { get; set; }
}
