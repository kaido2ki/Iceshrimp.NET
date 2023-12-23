using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("page_like")]
[Index("UserId", Name = "IDX_0e61efab7f88dbb79c9166dbb4")]
[Index("UserId", "PageId", Name = "IDX_4ce6fb9c70529b4c8ac46c9bfa", IsUnique = true)]
public class PageLike {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("pageId")] [StringLength(32)] public string PageId { get; set; } = null!;

	[ForeignKey("PageId")]
	[InverseProperty("PageLikes")]
	public virtual Page Page { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("PageLikes")]
	public virtual User User { get; set; } = null!;
}