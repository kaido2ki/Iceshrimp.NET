using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_pending")]
[Index("Code", IsUnique = true)]
public class UserPending
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("code")] [StringLength(128)] public string Code { get; set; } = null!;

	[Column("username")]
	[StringLength(128)]
	public string Username { get; set; } = null!;

	[Column("email")] [StringLength(128)] public string Email { get; set; } = null!;

	[Column("password")]
	[StringLength(128)]
	public string Password { get; set; } = null!;
}