using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("following")]
[Index("FolloweeId", Name = "IDX_24e0042143a18157b234df186c")]
[Index("FollowerId", "FolloweeId", Name = "IDX_307be5f1d1252e0388662acb96", IsUnique = true)]
[Index("FollowerHost", Name = "IDX_4ccd2239268ebbd1b35e318754")]
[Index("CreatedAt", Name = "IDX_582f8fab771a9040a12961f3e7")]
[Index("FollowerId", Name = "IDX_6516c5a6f3c015b4eed39978be")]
[Index("FolloweeHost", Name = "IDX_fcdafee716dfe9c3b5fde90f30")]
public class Following {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Following.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The followee user ID.
	/// </summary>
	[Column("followeeId")]
	[StringLength(32)]
	public string FolloweeId { get; set; } = null!;

	/// <summary>
	///     The follower user ID.
	/// </summary>
	[Column("followerId")]
	[StringLength(32)]
	public string FollowerId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerHost")]
	[StringLength(512)]
	public string? FollowerHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerInbox")]
	[StringLength(512)]
	public string? FollowerInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followerSharedInbox")]
	[StringLength(512)]
	public string? FollowerSharedInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeHost")]
	[StringLength(512)]
	public string? FolloweeHost { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeInbox")]
	[StringLength(512)]
	public string? FolloweeInbox { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("followeeSharedInbox")]
	[StringLength(512)]
	public string? FolloweeSharedInbox { get; set; }

	[ForeignKey("FolloweeId")]
	[InverseProperty("FollowingFollowees")]
	public virtual User Followee { get; set; } = null!;

	[ForeignKey("FollowerId")]
	[InverseProperty("FollowingFollowers")]
	public virtual User Follower { get; set; } = null!;
}