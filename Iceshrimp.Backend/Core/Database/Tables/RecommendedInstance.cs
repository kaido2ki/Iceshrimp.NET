using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("recommended_instance")]
public class RecommendedInstance
{
	[Key]
	[Column("host")]
	[StringLength(256)]
	public string Host { get; set; } = null!;
}
