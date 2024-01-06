using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("migrations")]
public class LegacyMigrations {
	[Key] [Column("id")] public int Id { get; set; }

	[Column("timestamp")] public long Timestamp { get; set; }

	[Column("name", TypeName = "character varying")]
	public string Name { get; set; } = null!;
}