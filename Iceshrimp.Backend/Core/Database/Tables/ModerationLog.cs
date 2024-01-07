﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("moderation_log")]
[Index("UserId")]
public class ModerationLog {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the ModerationLog.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("type")] [StringLength(128)] public string Type { get; set; } = null!;

	[Column("info", TypeName = "jsonb")] public string Info { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("ModerationLogs")]
	public virtual User User { get; set; } = null!;
}