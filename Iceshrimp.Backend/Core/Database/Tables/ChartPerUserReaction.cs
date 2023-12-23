using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__per_user_reaction")]
[Index("Date", "Group", Name = "IDX_229a41ad465f9205f1f5703291", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_229a41ad465f9205f1f57032910", IsUnique = true)]
public class ChartPerUserReaction {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___local_count")] public short LocalCount { get; set; }

	[Column("___remote_count")] public short RemoteCount { get; set; }
}