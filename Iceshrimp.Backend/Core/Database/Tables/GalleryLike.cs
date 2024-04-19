using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("gallery_like")]
[Index(nameof(UserId))]
[Index("UserId", "PostId", IsUnique = true)]
public class GalleryLike
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("postId")] [StringLength(32)] public string PostId { get; set; } = null!;

	[ForeignKey("PostId")]
	[InverseProperty(nameof(GalleryPost.GalleryLikes))]
	public virtual GalleryPost Post { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.GalleryLikes))]
	public virtual User User { get; set; } = null!;
}