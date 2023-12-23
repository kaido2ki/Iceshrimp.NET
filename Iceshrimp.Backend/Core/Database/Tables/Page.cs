using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("page")]
[Index("UserId", "Name", Name = "IDX_2133ef8317e4bdb839c0dcbf13", IsUnique = true)]
[Index("VisibleUserIds", Name = "IDX_90148bbc2bf0854428786bfc15")]
[Index("UserId", Name = "IDX_ae1d917992dd0c9d9bbdad06c4")]
[Index("UpdatedAt", Name = "IDX_af639b066dfbca78b01a920f8a")]
[Index("Name", Name = "IDX_b82c19c08afb292de4600d99e4")]
[Index("CreatedAt", Name = "IDX_fbb4297c927a9b85e9cefa2eb1")]
public class Page {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Page.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The updated date of the Page.
	/// </summary>
	[Column("updatedAt")]
	public DateTime UpdatedAt { get; set; }

	[Column("title")] [StringLength(256)] public string Title { get; set; } = null!;

	[Column("name")] [StringLength(256)] public string Name { get; set; } = null!;

	[Column("summary")]
	[StringLength(256)]
	public string? Summary { get; set; }

	[Column("alignCenter")] public bool AlignCenter { get; set; }

	[Column("font")] [StringLength(32)] public string Font { get; set; } = null!;

	/// <summary>
	///     The ID of author.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("eyeCatchingImageId")]
	[StringLength(32)]
	public string? EyeCatchingImageId { get; set; }

	[Column("content", TypeName = "jsonb")]
	public string Content { get; set; } = null!;

	[Column("variables", TypeName = "jsonb")]
	public string Variables { get; set; } = null!;

	[Column("visibleUserIds", TypeName = "character varying(32)[]")]
	public List<string> VisibleUserIds { get; set; } = null!;

	[Column("likedCount")] public int LikedCount { get; set; }

	[Column("hideTitleWhenPinned")] public bool HideTitleWhenPinned { get; set; }

	[Column("script")]
	[StringLength(16384)]
	public string Script { get; set; } = null!;

	[Column("isPublic")] public bool IsPublic { get; set; }

	[ForeignKey("EyeCatchingImageId")]
	[InverseProperty("Pages")]
	public virtual DriveFile? EyeCatchingImage { get; set; }

	[InverseProperty("Page")] public virtual ICollection<PageLike> PageLikes { get; set; } = new List<PageLike>();

	[ForeignKey("UserId")]
	[InverseProperty("Pages")]
	public virtual User User { get; set; } = null!;

	[InverseProperty("PinnedPage")] public virtual UserProfile? UserProfile { get; set; }
}