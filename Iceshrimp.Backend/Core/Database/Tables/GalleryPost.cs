using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("gallery_post")]
[Index("Tags")]
[Index("LikedCount")]
[Index("FileIds")]
[Index("CreatedAt")]
[Index("UserId")]
[Index("IsSensitive")]
[Index("UpdatedAt")]
public class GalleryPost
{
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

	[InverseProperty(nameof(GalleryLike.Post))]
	public virtual ICollection<GalleryLike> GalleryLikes { get; set; } = new List<GalleryLike>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.GalleryPosts))]
	public virtual User User { get; set; } = null!;
}