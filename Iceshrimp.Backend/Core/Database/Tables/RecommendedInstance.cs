using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("bubble_instance")]
public class BubbleInstance
{
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;
}
