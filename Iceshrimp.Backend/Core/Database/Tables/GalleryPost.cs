using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("gallery_post")]
[Index("Tags", Name = "IDX_05cca34b985d1b8edc1d1e28df")]
[Index("LikedCount", Name = "IDX_1a165c68a49d08f11caffbd206")]
[Index("FileIds", Name = "IDX_3ca50563facd913c425e7a89ee")]
[Index("CreatedAt", Name = "IDX_8f1a239bd077c8864a20c62c2c")]
[Index("UserId", Name = "IDX_985b836dddd8615e432d7043dd")]
[Index("IsSensitive", Name = "IDX_f2d744d9a14d0dfb8b96cb7fc5")]
[Index("UpdatedAt", Name = "IDX_f631d37835adb04792e361807c")]
public class GalleryPost {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the GalleryPost.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The updated date of the GalleryPost.
	/// </summary>
	[Column("updatedAt")]
	public DateTime UpdatedAt { get; set; }

	[Column("title")] [StringLength(256)] public string Title { get; set; } = null!;

	[Column("description")]
	[StringLength(2048)]
	public string? Description { get; set; }

	/// <summary>
	///     The ID of author.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("fileIds", TypeName = "character varying(32)[]")]
	public List<string> FileIds { get; set; } = null!;

	/// <summary>
	///     Whether the post is sensitive.
	/// </summary>
	[Column("isSensitive")]
	public bool IsSensitive { get; set; }

	[Column("likedCount")] public int LikedCount { get; set; }

	[Column("tags", TypeName = "character varying(128)[]")]
	public List<string> Tags { get; set; } = null!;

	[InverseProperty("Post")]
	public virtual ICollection<GalleryLike> GalleryLikes { get; set; } = new List<GalleryLike>();

	[ForeignKey("UserId")]
	[InverseProperty("GalleryPosts")]
	public virtual User User { get; set; } = null!;
}