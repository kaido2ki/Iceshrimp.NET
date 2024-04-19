﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("page")]
[Index("UserId", "Name", IsUnique = true)]
[Index(nameof(VisibleUserIds))]
[Index(nameof(UserId))]
[Index(nameof(UpdatedAt))]
[Index(nameof(Name))]
[Index(nameof(CreatedAt))]
public class Page
{
	[PgName("page_visibility_enum")]
	public enum PageVisibility
	{
		[PgName("public")]    Public,
		[PgName("followers")] Followers,
		[PgName("specified")] Specified
	}

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

	[Column("visibility")] public PageVisibility Visibility { get; set; }

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

	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
	[Column("content", TypeName = "jsonb")]
	public string Content { get; set; } = null!;

	//TODO: refactor this column (it's currently a Dictionary<string, any>, which is terrible) 
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
	[InverseProperty(nameof(DriveFile.Pages))]
	public virtual DriveFile? EyeCatchingImage { get; set; }

	[InverseProperty(nameof(PageLike.Page))]
	public virtual ICollection<PageLike> PageLikes { get; set; } = new List<PageLike>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.Pages))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(Tables.UserProfile.PinnedPage))]
	public virtual UserProfile? UserProfile { get; set; }
}