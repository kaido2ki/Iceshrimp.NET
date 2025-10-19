using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("promo_note")]
[Index(nameof(UserId))]
public class PromoNote
{
	[Key]
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("expiresAt")] public DateTime ExpiresAt { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.PromoNote))]
	public virtual Note Note { get; set; } = null!;

	private class EntityTypeConfiguration : IEntityTypeConfiguration<PromoNote>
	{
		public void Configure(EntityTypeBuilder<PromoNote> entity)
		{
			entity.Property(e => e.UserId).HasComment("[Denormalized]");

			entity.HasOne(d => d.Note)
			      .WithOne(p => p.PromoNote)
			      .OnDelete(DeleteBehavior.Cascade);
            
            // Reverse of the filter from Note
            // TODO: name this filter when we update to EF 10
            // https://learn.microsoft.com/en-us/ef/core/querying/filters?tabs=ef10#using-multiple-query-filters
            entity.HasQueryFilter(e => e.Note.Published);
		}
	}
}