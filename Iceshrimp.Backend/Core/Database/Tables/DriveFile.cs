using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("drive_file")]
[Index("IsLink", Name = "IDX_315c779174fe8247ab324f036e")]
[Index("Md5", Name = "IDX_37bb9a1b4585f8a3beb24c62d6")]
[Index("UserId", "FolderId", "Id", Name = "IDX_55720b33a61a7c806a8215b825")]
[Index("UserId", Name = "IDX_860fa6f6c7df5bb887249fba22")]
[Index("UserHost", Name = "IDX_92779627994ac79277f070c91e")]
[Index("Type", Name = "IDX_a40b8df8c989d7db937ea27cf6")]
[Index("IsSensitive", Name = "IDX_a7eba67f8b3fa27271e85d2e26")]
[Index("FolderId", Name = "IDX_bb90d1956dafc4068c28aa7560")]
[Index("WebpublicAccessKey", Name = "IDX_c55b2b7c284d9fef98026fc88e", IsUnique = true)]
[Index("CreatedAt", Name = "IDX_c8dfad3b72196dd1d6b5db168a")]
[Index("AccessKey", Name = "IDX_d85a184c2540d2deba33daf642", IsUnique = true)]
[Index("Uri", Name = "IDX_e5848eac4940934e23dbc17581")]
[Index("ThumbnailAccessKey", Name = "IDX_e74022ce9a074b3866f70e0d27", IsUnique = true)]
public class DriveFile {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the DriveFile.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string? UserId { get; set; }

	/// <summary>
	///     The host of owner. It will be null if the user in local.
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	/// <summary>
	///     The MD5 hash of the DriveFile.
	/// </summary>
	[Column("md5")]
	[StringLength(32)]
	public string Md5 { get; set; } = null!;

	/// <summary>
	///     The file name of the DriveFile.
	/// </summary>
	[Column("name")]
	[StringLength(256)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The content type (MIME) of the DriveFile.
	/// </summary>
	[Column("type")]
	[StringLength(128)]
	public string Type { get; set; } = null!;

	/// <summary>
	///     The file size (bytes) of the DriveFile.
	/// </summary>
	[Column("size")]
	public int Size { get; set; }

	/// <summary>
	///     The comment of the DriveFile.
	/// </summary>
	[Column("comment")]
	[StringLength(8192)]
	public string? Comment { get; set; }

	/// <summary>
	///     The any properties of the DriveFile. For example, it includes image width/height.
	/// </summary>
	[Column("properties", TypeName = "jsonb")]
	public string Properties { get; set; } = null!;

	[Column("storedInternal")] public bool StoredInternal { get; set; }

	/// <summary>
	///     The URL of the DriveFile.
	/// </summary>
	[Column("url")]
	[StringLength(512)]
	public string Url { get; set; } = null!;

	/// <summary>
	///     The URL of the thumbnail of the DriveFile.
	/// </summary>
	[Column("thumbnailUrl")]
	[StringLength(512)]
	public string? ThumbnailUrl { get; set; }

	/// <summary>
	///     The URL of the webpublic of the DriveFile.
	/// </summary>
	[Column("webpublicUrl")]
	[StringLength(512)]
	public string? WebpublicUrl { get; set; }

	[Column("accessKey")]
	[StringLength(256)]
	public string? AccessKey { get; set; }

	[Column("thumbnailAccessKey")]
	[StringLength(256)]
	public string? ThumbnailAccessKey { get; set; }

	[Column("webpublicAccessKey")]
	[StringLength(256)]
	public string? WebpublicAccessKey { get; set; }

	/// <summary>
	///     The URI of the DriveFile. it will be null when the DriveFile is local.
	/// </summary>
	[Column("uri")]
	[StringLength(512)]
	public string? Uri { get; set; }

	[Column("src")] [StringLength(512)] public string? Src { get; set; }

	/// <summary>
	///     The parent folder ID. If null, it means the DriveFile is located in root.
	/// </summary>
	[Column("folderId")]
	[StringLength(32)]
	public string? FolderId { get; set; }

	/// <summary>
	///     Whether the DriveFile is NSFW.
	/// </summary>
	[Column("isSensitive")]
	public bool IsSensitive { get; set; }

	/// <summary>
	///     Whether the DriveFile is direct link to remote server.
	/// </summary>
	[Column("isLink")]
	public bool IsLink { get; set; }

	/// <summary>
	///     The BlurHash string.
	/// </summary>
	[Column("blurhash")]
	[StringLength(128)]
	public string? Blurhash { get; set; }

	[Column("webpublicType")]
	[StringLength(128)]
	public string? WebpublicType { get; set; }

	[Column("requestHeaders", TypeName = "jsonb")]
	public string? RequestHeaders { get; set; }

	[Column("requestIp")]
	[StringLength(128)]
	public string? RequestIp { get; set; }

	[InverseProperty("Banner")] public virtual ICollection<Channel> Channels { get; set; } = new List<Channel>();

	[ForeignKey("FolderId")]
	[InverseProperty("DriveFiles")]
	public virtual DriveFolder? Folder { get; set; }

	[InverseProperty("File")]
	public virtual ICollection<MessagingMessage> MessagingMessages { get; set; } = new List<MessagingMessage>();

	[InverseProperty("EyeCatchingImage")] public virtual ICollection<Page> Pages { get; set; } = new List<Page>();

	[ForeignKey("UserId")]
	[InverseProperty("DriveFiles")]
	public virtual User? User { get; set; }

	[InverseProperty("Avatar")] public virtual User? UserAvatar { get; set; }

	[InverseProperty("Banner")] public virtual User? UserBanner { get; set; }
}