using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Shared.Schemas;

public class DriveFileResponse
{
	[J("id")]           public required string  Id           { get; set; }
	[J("url")]          public required string  Url          { get; set; }
	[J("thumbnailUrl")] public required string  ThumbnailUrl { get; set; }
	[J("filename")]     public required string  Filename     { get; set; }
	[J("contentType")]  public required string  ContentType  { get; set; }
	[J("sensitive")]    public required bool    Sensitive    { get; set; }
	[J("description")]  public required string? Description  { get; set; }
}