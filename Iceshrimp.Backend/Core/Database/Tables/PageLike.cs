using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("page_like")]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(PageId), IsUnique = true)]
public class PageLike
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("pageId")] [StringLength(32)] public string PageId { get; set; } = null!;

	[ForeignKey(nameof(PageId))]
	[InverseProperty(nameof(Tables.Page.PageLikes))]
	public virtual Page Page { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PageLikes))]
	public virtual User User { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<PageLike>
	{
		public void Configure(EntityTypeBuilder<PageLike> entity)
		{
			entity.HasOne(d => d.Page)
			      .WithMany(p => p.PageLikes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PageLikes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}