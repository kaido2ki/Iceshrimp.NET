using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel_following")]
[Index(nameof(FolloweeId))]
[Index(nameof(CreatedAt))]
[Index(nameof(FollowerId), nameof(FolloweeId), IsUnique = true)]
[Index(nameof(FollowerId))]
public class ChannelFollowing
{
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

	[ForeignKey(nameof(FolloweeId))]
	[InverseProperty(nameof(Channel.ChannelFollowings))]
	public virtual Channel Followee { get; set; } = null!;

	[ForeignKey(nameof(FollowerId))]
	[InverseProperty(nameof(User.ChannelFollowings))]
	public virtual User Follower { get; set; } = null!;
}