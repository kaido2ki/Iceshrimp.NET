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

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserSettings))]
	public virtual User User { get; set; } = null!;

	[Column("defaultNoteVisibility")]   public Note.NoteVisibility DefaultNoteVisibility   { get; set; }
	[Column("defaultRenoteVisibility")] public Note.NoteVisibility DefaultRenoteVisibility { get; set; }
	[Column("privateMode")]             public bool                PrivateMode             { get; set; }
	[Column("filterInaccessible")]      public bool                FilterInaccessible      { get; set; }
}