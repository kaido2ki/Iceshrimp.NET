using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("used_username")]
public class UsedUsername
{
	[Key]
	[Column("username")]
	[StringLength(128)]
	public string Username { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }
}