using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_publickey")]
[Index(nameof(KeyId), IsUnique = true)]
public class UserPublickey
{
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("keyId")] [StringLength(512)] public string KeyId { get; set; } = null!;

	[Column("keyPem")]
	[StringLength(4096)]
	public string KeyPem { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserPublickey))]
	public virtual User User { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserPublickey>
	{
		public void Configure(EntityTypeBuilder<UserPublickey> entity)
		{
			entity.HasOne(d => d.User)
			      .WithOne(p => p.UserPublickey)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}