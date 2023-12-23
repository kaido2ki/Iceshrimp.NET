using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_watching")]
[Index("NoteId", Name = "IDX_03e7028ab8388a3f5e3ce2a861")]
[Index("CreatedAt", Name = "IDX_318cdf42a9cfc11f479bd802bb")]
[Index("NoteUserId", Name = "IDX_44499765eec6b5489d72c4253b")]
[Index("UserId", "NoteId", Name = "IDX_a42c93c69989ce1d09959df4cf", IsUnique = true)]
[Index("UserId", Name = "IDX_b0134ec406e8d09a540f818288")]
public class NoteWatching {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the NoteWatching.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The watcher ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The target Note ID.
	/// </summary>
	[Column("noteId")]
	[StringLength(32)]
	public string NoteId { get; set; } = null!;

	/// <summary>
	///     [Denormalized]
	/// </summary>
	[Column("noteUserId")]
	[StringLength(32)]
	public string NoteUserId { get; set; } = null!;

	[ForeignKey("NoteId")]
	[InverseProperty("NoteWatchings")]
	public virtual Note Note { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("NoteWatchings")]
	public virtual User User { get; set; } = null!;
}