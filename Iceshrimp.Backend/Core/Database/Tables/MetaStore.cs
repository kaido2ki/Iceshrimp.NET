using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("meta_store")]
[Index("Key")]
public class MetaStore
{
	[Key]
	[Column("key")]
	[StringLength(128)]
	public string Key { get; set; } = null!;

	[Column("value")] public string Value { get; set; } = null!;
}