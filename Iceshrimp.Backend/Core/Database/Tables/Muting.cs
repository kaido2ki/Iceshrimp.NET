using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("muting")]
[Index(nameof(MuterId), nameof(MuteeId), IsUnique = true)]
[Index(nameof(MuterId))]
[Index(nameof(ExpiresAt))]
[Index(nameof(MuteeId))]
[Index(nameof(CreatedAt))]
public class Muting
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Muting.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The mutee user ID.
	/// </summary>
	[Column("muteeId")]
	[StringLength(32)]
	public string MuteeId { get; set; } = null!;

	/// <summary>
	///     The muter user ID.
	/// </summary>
	[Column("muterId")]
	[StringLength(32)]
	public string MuterId { get; set; } = null!;

	[Column("expiresAt")] public DateTime? ExpiresAt { get; set; }

	[ForeignKey(nameof(MuteeId))]
	[InverseProperty(nameof(User.IncomingMutes))]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey(nameof(MuterId))]
	[InverseProperty(nameof(User.OutgoingMutes))]
	public virtual User Muter { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Muting>
	{
		public void Configure(EntityTypeBuilder<Muting> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee)
			      .WithMany(p => p.IncomingMutes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Muter)
			      .WithMany(p => p.OutgoingMutes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}