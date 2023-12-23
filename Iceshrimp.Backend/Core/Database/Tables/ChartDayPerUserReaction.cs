using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__per_user_reaction")]
[Index("Date", "Group", Name = "IDX_d54b653660d808b118e36c184c", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_d54b653660d808b118e36c184c0", IsUnique = true)]
public class ChartDayPerUserReaction {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___local_count")] public short LocalCount { get; set; }

	[Column("___remote_count")] public short RemoteCount { get; set; }
}