using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("relay")]
[Index("Inbox", Name = "IDX_0d9a1738f2cf7f3b1c3334dfab", IsUnique = true)]
public class Relay {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("inbox")] [StringLength(512)] public string Inbox { get; set; } = null!;
}