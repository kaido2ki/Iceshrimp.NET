using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("cache_store")]
[Index("Expiry")]
public class CacheEntry
{
	[Key]
	[Column("key")]
	[StringLength(128)]
	public string Key { get; set; } = null!;

	[Column("value")] public string? Value { get; set; } = null!;

	[Column("expiry")] public DateTime? Expiry { get; set; } = null!;
	[Column("ttl")]    public TimeSpan? Ttl    { get; set; } = null!;
}