using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_unread")]
[Index("IsMentioned", Name = "IDX_25b1dd384bec391b07b74b861c")]
[Index("NoteUserId", Name = "IDX_29e8c1d579af54d4232939f994")]
[Index("UserId", Name = "IDX_56b0166d34ddae49d8ef7610bb")]
[Index("NoteChannelId", Name = "IDX_6a57f051d82c6d4036c141e107")]
[Index("IsSpecified", Name = "IDX_89a29c9237b8c3b6b3cbb4cb30")]
[Index("UserId", "NoteId", Name = "IDX_d908433a4953cc13216cd9c274", IsUnique = true)]
[Index("NoteId", Name = "IDX_e637cba4dc4410218c4251260e")]
public class NoteUnread {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteUserId")]
	[StringLength(32)]
	public string NoteUserId { get; set; } = null!;

	[Column("isSpecified")] public bool IsSpecified { get; set; }

	[Column("isMentioned")] public bool IsMentioned { get; set; }

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteChannelId")]
	[StringLength(32)]
	public string? NoteChannelId { get; set; }

	[ForeignKey("NoteId")]
	[InverseProperty("NoteUnreads")]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("NoteUnreads")]
	public virtual User User { get; set; } = null!;
}