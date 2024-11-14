using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("gallery_like")]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(PostId), IsUnique = true)]
public class GalleryLike
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("postId")] [StringLength(32)] public string PostId { get; set; } = null!;

	[ForeignKey(nameof(PostId))]
	[InverseProperty(nameof(GalleryPost.GalleryLikes))]
	public virtual GalleryPost Post { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.GalleryLikes))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<GalleryLike>
	{
		public void Configure(EntityTypeBuilder<GalleryLike> entity)
		{
			entity.HasOne(d => d.Post)
			      .WithMany(p => p.GalleryLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.GalleryLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}