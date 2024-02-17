using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("poll")]
[Index("UserId")]
[Index("UserHost")]
public class Poll
{
	[Key]
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	[Column("expiresAt")] public DateTime? ExpiresAt { get; set; }

	[Column("multiple")] public bool Multiple { get; set; }

	[Column("choices", TypeName = "character varying(256)[]")]
	public List<string> Choices { get; set; } = null!;

	[Column("votes")] public List<int> Votes { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; }

	[ForeignKey("NoteId")]
	[InverseProperty(nameof(Tables.Note.Poll))]
	public virtual Note Note { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteVisibility")]
	public Note.NoteVisibility NoteVisibility { get; set; }
}