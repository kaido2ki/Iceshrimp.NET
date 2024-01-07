using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_favorite")]
[Index("UserId", "NoteId", IsUnique = true)]
[Index("UserId")]
public class NoteFavorite {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the NoteFavorite.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("NoteFavorites")]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("NoteFavorites")]
	public virtual User User { get; set; } = null!;
}