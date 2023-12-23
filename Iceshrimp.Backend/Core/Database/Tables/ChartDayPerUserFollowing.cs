using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__per_user_following")]
[Index("Date", "Group", Name = "IDX_e4849a3231f38281280ea4c0ee", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_e4849a3231f38281280ea4c0eee", IsUnique = true)]
public class ChartDayPerUserFollowing {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___local_followings_total")] public int LocalFollowingsTotal { get; set; }

	[Column("___local_followings_inc")] public short LocalFollowingsInc { get; set; }

	[Column("___local_followings_dec")] public short LocalFollowingsDec { get; set; }

	[Column("___local_followers_total")] public int LocalFollowersTotal { get; set; }

	[Column("___local_followers_inc")] public short LocalFollowersInc { get; set; }

	[Column("___local_followers_dec")] public short LocalFollowersDec { get; set; }

	[Column("___remote_followings_total")] public int RemoteFollowingsTotal { get; set; }

	[Column("___remote_followings_inc")] public short RemoteFollowingsInc { get; set; }

	[Column("___remote_followings_dec")] public short RemoteFollowingsDec { get; set; }

	[Column("___remote_followers_total")] public int RemoteFollowersTotal { get; set; }

	[Column("___remote_followers_inc")] public short RemoteFollowersInc { get; set; }

	[Column("___remote_followers_dec")] public short RemoteFollowersDec { get; set; }
}