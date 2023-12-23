using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("password_reset_request")]
[Index("Token", Name = "IDX_0b575fa9a4cfe638a925949285", IsUnique = true)]
[Index("UserId", Name = "IDX_4bb7fd4a34492ae0e6cc8d30ac")]
public class PasswordResetRequest {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("token")] [StringLength(256)] public string Token { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("PasswordResetRequests")]
	public virtual User User { get; set; } = null!;
}