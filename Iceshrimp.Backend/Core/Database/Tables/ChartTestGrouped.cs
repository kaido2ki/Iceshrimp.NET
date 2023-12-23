using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__test_grouped")]
[Index("Date", "Group", Name = "IDX_b14489029e4b3aaf4bba5fb524", IsUnique = true)]
public class ChartTestGrouped {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string? Group { get; set; }

	[Column("___foo_total")] public long FooTotal { get; set; }

	[Column("___foo_inc")] public long FooInc { get; set; }

	[Column("___foo_dec")] public long FooDec { get; set; }
}