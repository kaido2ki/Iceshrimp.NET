namespace Iceshrimp.Shared.Schemas.Web;

public class DriveFolderRequest
{
    public required string  Name     { get; set; }
    public required string? ParentId { get; set; }
}
