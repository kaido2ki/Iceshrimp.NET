using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_edit")]
[Index(nameof(NoteId))]
public class NoteEdit
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The ID of note.
	/// </summary>
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("text")] public string? Text { get; set; }

	[Column("cw")] [StringLength(512)] public string? Cw { get; set; }

	[Column("fileIds", TypeName = "character varying(32)[]")]
	public List<string> FileIds { get; set; } = null!;

	/// <summary>
	///     The updated date of the Note.
	/// </summary>
	[Column("updatedAt")]
	public DateTime UpdatedAt { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.NoteEdits))]
	public virtual Note Note { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<NoteEdit>
	{
		public void Configure(EntityTypeBuilder<NoteEdit> entity)
		{
			entity.Property(e => e.FileIds).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.NoteId).HasComment("The ID of note.");
			entity.Property(e => e.UpdatedAt).HasComment("The updated date of the Note.");

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.NoteEdits)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}