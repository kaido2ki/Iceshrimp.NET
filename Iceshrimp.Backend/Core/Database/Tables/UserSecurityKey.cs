using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_security_key")]
[Index(nameof(PublicKey))]
[Index(nameof(UserId))]
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

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserSecurityKeys))]
	public virtual User User { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserSecurityKey>
	{
		public void Configure(EntityTypeBuilder<UserSecurityKey> entity)
		{
			entity.Property(e => e.Id).HasComment("Variable-length id given to navigator.credentials.get()");
			entity.Property(e => e.LastUsed)
			      .HasComment("The date of the last time the UserSecurityKey was successfully validated.");
			entity.Property(e => e.Name).HasComment("User-defined name for this key");
			entity.Property(e => e.PublicKey)
			      .HasComment("Variable-length public key used to verify attestations (hex-encoded).");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserSecurityKeys)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}