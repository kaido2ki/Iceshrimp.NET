namespace Iceshrimp.Shared.Schemas.Web;

public class DriveMoveRequest(string? folderId)
{
    public required string? FolderId { get; set; } = folderId;
}
