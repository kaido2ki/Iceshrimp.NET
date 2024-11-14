using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("clip_note")]
[Index(nameof(NoteId), nameof(ClipId), IsUnique = true)]
[Index(nameof(NoteId))]
[Index(nameof(ClipId))]
public class ClipNote
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The note ID.
	/// </summary>
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	/// <summary>
	///     The clip ID.
	/// </summary>
	[Column("clipId")]
	[StringLength(32)]
	public string ClipId { get; set; } = null!;

	[ForeignKey(nameof(ClipId))]
	[InverseProperty(nameof(Tables.Clip.ClipNotes))]
	public virtual Clip Clip { get; set; } = null!;

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.ClipNotes))]
	public virtual Note Note { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<ClipNote>
	{
		public void Configure(EntityTypeBuilder<ClipNote> entity)
		{
			entity.Property(e => e.ClipId).HasComment("The clip ID.");
			entity.Property(e => e.NoteId).HasComment("The note ID.");

			entity.HasOne(d => d.Clip)
			      .WithMany(p => p.ClipNotes)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.ClipNotes)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}