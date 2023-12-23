using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart_day__per_user_drive")]
[Index("Date", "Group", Name = "IDX_62aa5047b5aec92524f24c701d", IsUnique = true)]
[Index("Date", "Group", Name = "UQ_62aa5047b5aec92524f24c701d7", IsUnique = true)]
public class ChartDayPerUserDrive {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string Group { get; set; } = null!;

	[Column("___totalCount")] public int TotalCount { get; set; }

	[Column("___totalSize")] public int TotalSize { get; set; }

	[Column("___incCount")] public short IncCount { get; set; }

	[Column("___incSize")] public int IncSize { get; set; }

	[Column("___decCount")] public short DecCount { get; set; }

	[Column("___decSize")] public int DecSize { get; set; }
}