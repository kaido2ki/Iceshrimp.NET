using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_keypair")]
public class UserKeypair
{
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("publicKey")]
	[StringLength(4096)]
	public string PublicKey { get; set; } = null!;

	[Column("privateKey")]
	[StringLength(4096)]
	public string PrivateKey { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserKeypair))]
	public virtual User User { get; set; } = null!;
}