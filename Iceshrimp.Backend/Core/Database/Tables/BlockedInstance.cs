using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("blocked_instance")]
public class BlockedInstance
{
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;

	[Column("reason")]
	[StringLength(1024)]
	public string? Reason { get; set; } = null!;

	[Column("imported")] public bool IsImported { get; set; }
}