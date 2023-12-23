using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("html_user_cache_entry")]
public class HtmlUserCacheEntry {
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("updatedAt")] public DateTime? UpdatedAt { get; set; }

	[Column("bio")] public string? Bio { get; set; }

	[Column("fields", TypeName = "jsonb")] public string Fields { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("HtmlUserCacheEntry")]
	public virtual User User { get; set; } = null!;
}