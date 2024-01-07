using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_note_pin")]
[Index("UserId", "NoteId", Name = "IDX_410cd649884b501c02d6e72738", IsUnique = true)]
[Index("UserId", Name = "IDX_bfbc6f79ba4007b4ce5097f08d")]
public class UserNotePin {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserNotePins.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("UserNotePins")]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("UserNotePins")]
	public virtual User User { get; set; } = null!;
}