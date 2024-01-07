using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_reaction")]
[Index("CreatedAt")]
[Index("UserId")]
[Index("NoteId")]
[Index("UserId", "NoteId", IsUnique = true)]
public class NoteReaction {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the NoteReaction.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[Column("reaction")]
	[StringLength(260)]
	public string Reaction { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("NoteReactions")]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("NoteReactions")]
	public virtual User User { get; set; } = null!;
}