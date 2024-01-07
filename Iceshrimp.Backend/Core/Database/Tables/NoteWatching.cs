﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("note_watching")]
[Index("NoteId")]
[Index("CreatedAt")]
[Index("NoteUserId")]
[Index("UserId", "NoteId", IsUnique = true)]
[Index("UserId")]
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