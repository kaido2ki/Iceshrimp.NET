using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_security_key")]
[Index("PublicKey", Name = "IDX_0d7718e562dcedd0aa5cf2c9f7")]
[Index("UserId", Name = "IDX_ff9ca3b5f3ee3d0681367a9b44")]
public class UserSecurityKey {
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
	[InverseProperty("UserSecurityKeys")]
	public virtual User User { get; set; } = null!;
}