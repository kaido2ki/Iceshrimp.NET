namespace Iceshrimp.Shared.Schemas;

public class UpdateDriveFileRequest
{
	public string? Filename    { get; set; }
	public bool?   Sensitive   { get; set; }
	public string? Description { get; set; }
}