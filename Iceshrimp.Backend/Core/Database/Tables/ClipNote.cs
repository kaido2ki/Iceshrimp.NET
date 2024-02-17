using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("clip_note")]
[Index("NoteId", "ClipId", IsUnique = true)]
[Index("NoteId")]
[Index("ClipId")]
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

	[ForeignKey("ClipId")]
	[InverseProperty(nameof(Tables.Clip.ClipNotes))]
	public virtual Clip Clip { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty(nameof(Tables.Note.ClipNotes))]
	public virtual Note Note { get; set; } = null!;
}