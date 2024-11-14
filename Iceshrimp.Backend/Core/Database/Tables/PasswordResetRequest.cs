using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("password_reset_request")]
[Index(nameof(Token), IsUnique = true)]
[Index(nameof(UserId))]
public class PasswordResetRequest
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("token")] [StringLength(256)] public string Token { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PasswordResetRequests))]
	public virtual User User { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<PasswordResetRequest>
	{
		public void Configure(EntityTypeBuilder<PasswordResetRequest> entity)
		{
			entity.HasOne(d => d.User)
			      .WithMany(p => p.PasswordResetRequests)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}