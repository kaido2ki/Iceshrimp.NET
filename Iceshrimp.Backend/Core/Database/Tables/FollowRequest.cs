using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("follow_request")]
[Index("FolloweeId", Name = "IDX_12c01c0d1a79f77d9f6c15fadd")]
[Index("FollowerId", Name = "IDX_a7fd92dd6dc519e6fb435dd108")]
[Index("FollowerId", "FolloweeId", Name = "IDX_d54a512b822fac7ed52800f6b4", IsUnique = true)]
public class FollowRequest {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the FollowRequest.
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
	///     id of Follow Activity.
	/// </summary>
	[Column("requestId")]
	[StringLength(128)]
	public string? RequestId { get; set; }

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
	[InverseProperty("FollowRequestFollowees")]
	public virtual User Followee { get; set; } = null!;

	[ForeignKey("FollowerId")]
	[InverseProperty("FollowRequestFollowers")]
	public virtual User Follower { get; set; } = null!;

	[InverseProperty("FollowRequest")]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}