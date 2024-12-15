using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class DriveFileResponse : IIdentifiable
{
	public required string  Id           { get; set; }
	public required string  Url          { get; set; }
	public required string  ThumbnailUrl { get; set; }
	public required string  Filename     { get; set; }
	public required string  ContentType  { get; set; }
	public required bool    Sensitive    { get; set; }
	public required string? Description  { get; set; }
}

public class DriveFolderResponse
{
	public required string?                   Id       { get; set; }
	public required string?                   Name     { get; set; }
	public required string?                   ParentId { get; set; }
	public          List<DriveFileResponse>   Files    { get; set; } = [];
	public          List<DriveFolderResponse> Folders  { get; set; } = [];
}