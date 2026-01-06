namespace Iceshrimp.Shared.Schemas.Web;

public class DriveFolderRequest
{
    public required string Name { get; set; }

    /// <summary>
    /// The folder that this folder should be inside
    /// </summary>
    public required string? ParentId { get; set; }
}
