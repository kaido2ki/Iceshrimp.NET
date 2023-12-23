﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("notification")]
[Index("IsRead", Name = "IDX_080ab397c379af09b9d2169e5b")]
[Index("NotifierId", Name = "IDX_3b4e96eec8d36a8bbb9d02aa71")]
[Index("NotifieeId", Name = "IDX_3c601b70a1066d2c8b517094cb")]
[Index("CreatedAt", Name = "IDX_b11a5e627c41d4dc3170f1d370")]
[Index("AppAccessTokenId", Name = "IDX_e22bf6bda77b6adc1fd9e75c8c")]
public class Notification {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Notification.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The ID of recipient user of the Notification.
	/// </summary>
	[Column("notifieeId")]
	[StringLength(32)]
	public string NotifieeId { get; set; } = null!;

	/// <summary>
	///     The ID of sender user of the Notification.
	/// </summary>
	[Column("notifierId")]
	[StringLength(32)]
	public string? NotifierId { get; set; }

	/// <summary>
	///     Whether the notification was read.
	/// </summary>
	[Column("isRead")]
	public bool IsRead { get; set; }

	[Column("noteId")] [StringLength(32)] public string? NoteId { get; set; }

	[Column("reaction")]
	[StringLength(128)]
	public string? Reaction { get; set; }

	[Column("choice")] public int? Choice { get; set; }

	[Column("followRequestId")]
	[StringLength(32)]
	public string? FollowRequestId { get; set; }

	[Column("userGroupInvitationId")]
	[StringLength(32)]
	public string? UserGroupInvitationId { get; set; }

	[Column("customBody")]
	[StringLength(2048)]
	public string? CustomBody { get; set; }

	[Column("customHeader")]
	[StringLength(256)]
	public string? CustomHeader { get; set; }

	[Column("customIcon")]
	[StringLength(1024)]
	public string? CustomIcon { get; set; }

	[Column("appAccessTokenId")]
	[StringLength(32)]
	public string? AppAccessTokenId { get; set; }

	[ForeignKey("AppAccessTokenId")]
	[InverseProperty("Notifications")]
	public virtual AccessToken? AppAccessToken { get; set; }

	[ForeignKey("FollowRequestId")]
	[InverseProperty("Notifications")]
	public virtual FollowRequest? FollowRequest { get; set; }

	[ForeignKey("NoteId")]
	[InverseProperty("Notifications")]
	public virtual Note? Note { get; set; }

	[ForeignKey("NotifieeId")]
	[InverseProperty("NotificationNotifiees")]
	public virtual User Notifiee { get; set; } = null!;

	[ForeignKey("NotifierId")]
	[InverseProperty("NotificationNotifiers")]
	public virtual User? Notifier { get; set; }

	[ForeignKey("UserGroupInvitationId")]
	[InverseProperty("Notifications")]
	public virtual UserGroupInvitation? UserGroupInvitation { get; set; }
}