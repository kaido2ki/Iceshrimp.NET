using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("channel_following")]
[Index("FolloweeId", Name = "IDX_0e43068c3f92cab197c3d3cd86")]
[Index("CreatedAt", Name = "IDX_11e71f2511589dcc8a4d3214f9")]
[Index("FollowerId", "FolloweeId", Name = "IDX_2e230dd45a10e671d781d99f3e", IsUnique = true)]
[Index("FollowerId", Name = "IDX_6d8084ec9496e7334a4602707e")]
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