using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Shared.Schemas;

public class UpdateDriveFileRequest
{
	[J("filename")]    public string? Filename    { get; set; }
	[J("sensitive")]   public bool?   Sensitive   { get; set; }
	[J("description")] public string? Description { get; set; }
}