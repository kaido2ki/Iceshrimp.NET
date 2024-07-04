namespace Iceshrimp.Shared.Schemas.Web;

public class UpdateDriveFileRequest
{
	public string? Filename    { get; set; }
	public bool?   Sensitive   { get; set; }
	public string? Description { get; set; }
}