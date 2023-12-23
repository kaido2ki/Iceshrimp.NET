using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__test")]
[Index("Date", "Group", Name = "IDX_a319e5dbf47e8a17497623beae", IsUnique = true)]
public class ChartTest {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string? Group { get; set; }

	[Column("___foo_total")] public long FooTotal { get; set; }

	[Column("___foo_inc")] public long FooInc { get; set; }

	[Column("___foo_dec")] public long FooDec { get; set; }
}