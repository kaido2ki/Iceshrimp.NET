namespace Iceshrimp.Shared.Schemas.Web;

public class DriveStatusResponse
{
    public required int FileCount { get; set; }

    /// <summary>
    /// Sum of the size of every file in the user's Drive in bytes
    /// </summary>
    public required long UsedSize { get; set; }
}
