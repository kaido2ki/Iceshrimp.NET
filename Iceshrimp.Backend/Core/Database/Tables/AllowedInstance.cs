using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("allowed_instance")]
public class AllowedInstance {
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;

	[Column("imported")] public bool IsImported { get; set; }
}