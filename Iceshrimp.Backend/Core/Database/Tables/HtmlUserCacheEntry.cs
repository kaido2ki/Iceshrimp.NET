using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("html_user_cache_entry")]
public class HtmlUserCacheEntry
{
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("updatedAt")] public DateTime? UpdatedAt { get; set; }

	[Column("bio")] public string? Bio { get; set; }

	[Column("fields", TypeName = "jsonb")] public Field[] Fields { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.HtmlUserCacheEntry))]
	public virtual User User { get; set; } = null!;
}