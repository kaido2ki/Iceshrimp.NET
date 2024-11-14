using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("renote_muting")]
[Index(nameof(MuterId), nameof(MuteeId), IsUnique = true)]
[Index(nameof(MuterId))]
[Index(nameof(MuteeId))]
[Index(nameof(CreatedAt))]
public class RenoteMuting
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

	[ForeignKey(nameof(MuteeId))]
	[InverseProperty(nameof(User.RenoteMutingMutees))]
	public virtual User Mutee { get; set; } = null!;

	[ForeignKey(nameof(MuterId))]
	[InverseProperty(nameof(User.RenoteMutingMuters))]
	public virtual User Muter { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<RenoteMuting>
	{
		public void Configure(EntityTypeBuilder<RenoteMuting> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Muting.");
			entity.Property(e => e.MuteeId).HasComment("The mutee user ID.");
			entity.Property(e => e.MuterId).HasComment("The muter user ID.");

			entity.HasOne(d => d.Mutee)
			      .WithMany(p => p.RenoteMutingMutees)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Muter)
			      .WithMany(p => p.RenoteMutingMuters)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}