﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("announcement_read")]
[Index("AnnouncementId", Name = "IDX_603a7b1e7aa0533c6c88e9bfaf")]
[Index("UserId", Name = "IDX_8288151386172b8109f7239ab2")]
[Index("UserId", "AnnouncementId", Name = "IDX_924fa71815cfa3941d003702a0", IsUnique = true)]
public class AnnouncementRead {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("announcementId")]
	[StringLength(32)]
	public string AnnouncementId { get; set; } = null!;

	/// <summary>
	///     The created date of the AnnouncementRead.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[ForeignKey("AnnouncementId")]
	[InverseProperty("AnnouncementReads")]
	public virtual Announcement Announcement { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("AnnouncementReads")]
	public virtual User User { get; set; } = null!;
}