using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_security_key")]
[Index("PublicKey")]
[Index("UserId")]
public class UserSecurityKey
{
	/// <summary>
	///     Variable-length id given to navigator.credentials.get()
	/// </summary>
	[Key]
	[Column("id", TypeName = "character varying")]
	public string Id { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	/// <summary>
	///     Variable-length public key used to verify attestations (hex-encoded).
	/// </summary>
	[Column("publicKey", TypeName = "character varying")]
	public string PublicKey { get; set; } = null!;

	/// <summary>
	///     The date of the last time the UserSecurityKey was successfully validated.
	/// </summary>
	[Column("lastUsed")]
	public DateTime LastUsed { get; set; }

	/// <summary>
	///     User-defined name for this key
	/// </summary>
	[Column("name")]
	[StringLength(30)]
	public string Name { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.UserSecurityKeys))]
	public virtual User User { get; set; } = null!;
}