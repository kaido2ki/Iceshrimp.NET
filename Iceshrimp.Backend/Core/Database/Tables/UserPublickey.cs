using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_publickey")]
[Index("KeyId", Name = "IDX_171e64971c780ebd23fae140bb", IsUnique = true)]
public class UserPublickey {
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("keyId")] [StringLength(512)] public string KeyId { get; set; } = null!;

	[Column("keyPem")]
	[StringLength(4096)]
	public string KeyPem { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("UserPublickey")]
	public virtual User User { get; set; } = null!;
}