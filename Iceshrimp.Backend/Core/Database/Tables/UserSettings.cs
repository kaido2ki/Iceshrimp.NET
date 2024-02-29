using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_settings")]
public class UserSettings
{
	[Key]
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.UserSettings))]
	public virtual User User { get; set; } = null!;

	[Column("defaultNoteVisibility")] public Note.NoteVisibility DefaultNoteVisibility { get; set; }
	[Column("privateMode")]           public bool                PrivateMode           { get; set; }
}