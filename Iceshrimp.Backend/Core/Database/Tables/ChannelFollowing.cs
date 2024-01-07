using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel_following")]
[Index("FolloweeId")]
[Index("CreatedAt")]
[Index("FollowerId", "FolloweeId", IsUnique = true)]
[Index("FollowerId")]
public class ChannelFollowing {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the ChannelFollowing.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The followee channel ID.
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

	[ForeignKey("FolloweeId")]
	[InverseProperty("ChannelFollowings")]
	public virtual Channel Followee { get; set; } = null!;

	[ForeignKey("FollowerId")]
	[InverseProperty("ChannelFollowings")]
	public virtual User Follower { get; set; } = null!;
}