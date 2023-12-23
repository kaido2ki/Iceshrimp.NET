using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__per_user_notes")]
[Index("Date", "Group", Name = "IDX_c5545d4b31cdc684034e33b81c", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_c5545d4b31cdc684034e33b81c3", IsUnique = true)]
public class ChartDayPerUserNote {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___total")] public int Total { get; set; }

	[Column("___inc")] public short Inc { get; set; }

	[Column("___dec")] public short Dec { get; set; }

	[Column("___diffs_normal")] public short DiffsNormal { get; set; }

	[Column("___diffs_reply")] public short DiffsReply { get; set; }

	[Column("___diffs_renote")] public short DiffsRenote { get; set; }

	[Column("___diffs_withFile")] public short DiffsWithFile { get; set; }
}