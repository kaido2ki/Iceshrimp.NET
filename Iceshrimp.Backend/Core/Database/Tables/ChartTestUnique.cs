using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("__chart__test_unique")]
[Index("Date", "Group", Name = "IDX_a0cd75442dd10d0643a17c4a49", IsUnique = true)]
public class ChartTestUnique {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("date")] public int Date { get; set; }

	[Column("group")] [StringLength(128)] public string? Group { get; set; }

	[Column("___foo", TypeName = "character varying[]")]
	public List<string> Foo { get; set; } = null!;
}