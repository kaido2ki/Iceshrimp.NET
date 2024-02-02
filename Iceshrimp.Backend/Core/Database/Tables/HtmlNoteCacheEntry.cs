using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("html_note_cache_entry")]
public class HtmlNoteCacheEntry {
	[Key]
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("updatedAt")] public DateTime? UpdatedAt { get; set; }

	[Column("content")] public string? Content { get; set; }

	[ForeignKey("NoteId")]
	[InverseProperty(nameof(Tables.Note.HtmlNoteCacheEntry))]
	public virtual Note Note { get; set; } = null!;
}