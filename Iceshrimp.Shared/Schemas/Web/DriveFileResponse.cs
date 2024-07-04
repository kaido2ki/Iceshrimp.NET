namespace Iceshrimp.Shared.Schemas.Web;

public class DriveFileResponse
{
	public required string  Id           { get; set; }
	public required string  Url          { get; set; }
	public required string  ThumbnailUrl { get; set; }
	public required string  Filename     { get; set; }
	public required string  ContentType  { get; set; }
	public required bool    Sensitive    { get; set; }
	public required string? Description  { get; set; }
}