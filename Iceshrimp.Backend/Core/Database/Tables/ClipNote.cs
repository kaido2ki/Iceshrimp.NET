using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("clip_note")]
[Index("NoteId", "ClipId", Name = "IDX_6fc0ec357d55a18646262fdfff", IsUnique = true)]
[Index("NoteId", Name = "IDX_a012eaf5c87c65da1deb5fdbfa")]
[Index("ClipId", Name = "IDX_ebe99317bbbe9968a0c6f579ad")]
public class ClipNote {
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
	[InverseProperty("ClipNotes")]
	public virtual Clip Clip { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("ClipNotes")]
	public virtual Note Note { get; set; } = null!;
}