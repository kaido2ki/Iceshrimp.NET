using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("poll_vote")]
[Index(nameof(CreatedAt))]
[Index("UserId", "NoteId", "Choice", IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(NoteId))]
public class PollVote
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the PollVote.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("noteId")] [StringLength(32)] public string NoteId { get; set; } = null!;

	[Column("choice")] public int Choice { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.PollVotes))]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.PollVotes))]
	public virtual User User { get; set; } = null!;
}