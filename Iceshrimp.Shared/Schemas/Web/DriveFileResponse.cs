using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Shared.Schemas.Web;

public class DriveFileResponse : IIdentifiable
{
    public required string Id           { get; set; }
    public required string Url          { get; set; }
    public required string ThumbnailUrl { get; set; }
    public required string Filename     { get; set; }

    /// <summary>
    /// File MIME Type
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Whether the file should be blurred
    /// </summary>
    public required bool Sensitive { get; set; }

    /// <summary>
    /// File alt text
    /// </summary>
    public required string? Description { get; set; }

    /// <summary>
    /// Whether the file is used as the user's avatar
    /// </summary>
    public required bool IsAvatar { get; set; }

    /// <summary>
    /// Whether the file is used as the user's banner
    /// </summary>
    public required bool IsBanner { get; set; }
}

public class DriveFolderResponse
{
    public required string? Id   { get; set; }
    public required string? Name { get; set; }

    /// <summary>
    /// The folder that this folder is inside
    /// </summary>
    public required string? ParentId { get; set; }

	/// <summary>
	/// Folder path excluding the root and current folder. If the first <c>parentId</c> isn't <c>null</c> then the path has been truncated.
	/// </summary>
	public List<DrivePathEntry>? Path { get; set; }

	public List<DriveFileResponse>   Files         { get; set; } = [];
	public List<DriveFolderResponse> Folders       { get; set; } = [];
}

public class DrivePathEntry
{
	public required string  Id       { get; set; }
	public required string  Name     { get; set; }
	public required string? ParentId { get; set; }
}
