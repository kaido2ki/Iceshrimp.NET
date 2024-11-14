using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("promo_read")]
[Index(nameof(UserId), nameof(NoteId), IsUnique = true)]
[Index(nameof(UserId))]
public class PromoRead
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the PromoRead.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.PromoReads))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PromoReads))]
	public virtual User User { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<PromoRead>
	{
		public void Configure(EntityTypeBuilder<PromoRead> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the PromoRead.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.PromoReads)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.PromoReads)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}