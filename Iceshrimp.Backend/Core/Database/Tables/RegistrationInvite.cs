using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("registration_invite")]
[Index(nameof(Code), IsUnique = true)]
public class RegistrationInvite
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("code")] [StringLength(64)] public string Code { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<RegistrationInvite>
	{
		public void Configure(EntityTypeBuilder<RegistrationInvite> entity) { }
	}
}