using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("session")]
[Index(nameof(Token))]
public class Session : IIdentifiable
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the OAuth token
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	/// <summary>
	///     The authorization token
	/// </summary>
	[Column("token")]
	[StringLength(64)]
	public string Token { get; set; } = null!;

	/// <summary>
	///     Whether or not the token has been activated (i.e. 2fa has been confirmed)
	/// </summary>
	[Column("active")]
	public bool Active { get; set; }

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.Sessions))]
	public virtual User User { get; set; } = null!;

	[Column("lastActiveDate")] public DateTime? LastActiveDate { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Session>
	{
		public void Configure(EntityTypeBuilder<Session> entity)
		{
			entity.Property(e => e.Active)
			      .HasComment("Whether or not the token has been activated (i.e. 2fa has been confirmed)");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.Token).HasComment("The authorization token");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Sessions)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}