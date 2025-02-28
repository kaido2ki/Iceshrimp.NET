﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("notification")]
[Index(nameof(Type))]
[Index(nameof(IsRead))]
[Index(nameof(NotifierId))]
[Index(nameof(NotifieeId))]
[Index(nameof(CreatedAt))]
[Index(nameof(AppAccessTokenId))]
[Index(nameof(MastoId))]
[Index(nameof(NoteId))]
public class Notification : IEntity
{
	[PgName("notification_type_enum")]
	public enum NotificationType
	{
		[PgName("follow")]                Follow,
		[PgName("mention")]               Mention,
		[PgName("reply")]                 Reply,
		[PgName("renote")]                Renote,
		[PgName("quote")]                 Quote,
		[PgName("like")]                  Like,
		[PgName("reaction")]              Reaction,
		[PgName("pollVote")]              PollVote,
		[PgName("pollEnded")]             PollEnded,
		[PgName("receiveFollowRequest")]  FollowRequestReceived,
		[PgName("followRequestAccepted")] FollowRequestAccepted,
		[PgName("groupInvited")]          GroupInvited,
		[PgName("app")]                   App,
		[PgName("edit")]                  Edit,
		[PgName("bite")]                  Bite
	}

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

	[Column("type")] public NotificationType Type { get; set; }

	[Column("noteId")] [StringLength(32)] public string? NoteId { get; set; }
	[Column("biteId")] [StringLength(32)] public string? BiteId { get; set; }

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

	[ForeignKey(nameof(FollowRequestId))]
	[InverseProperty(nameof(Tables.FollowRequest.Notifications))]
	public virtual FollowRequest? FollowRequest { get; set; }

	[ForeignKey(nameof(NoteId))]
	[InverseProperty(nameof(Tables.Note.Notifications))]
	public virtual Note? Note { get; set; }

	[ForeignKey(nameof(BiteId))] public virtual Bite? Bite { get; set; }

	[ForeignKey(nameof(NotifieeId))]
	[InverseProperty(nameof(User.NotificationNotifiees))]
	public virtual User Notifiee { get; set; } = null!;

	[ForeignKey(nameof(NotifierId))]
	[InverseProperty(nameof(User.NotificationNotifiers))]
	public virtual User? Notifier { get; set; }

	[ForeignKey(nameof(UserGroupInvitationId))]
	[InverseProperty(nameof(Tables.UserGroupInvitation.Notifications))]
	public virtual UserGroupInvitation? UserGroupInvitation { get; set; }

	[Column("masto_id")]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public long MastoId { get; set; }

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	public Notification WithPrecomputedNoteVisibilities(bool reply, bool renote, bool renoteRenote)
	{
		Note = Note?.WithPrecomputedVisibilities(reply, renote, renoteRenote);
		return this;
	}
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Notification>
	{
		public void Configure(EntityTypeBuilder<Notification> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Notification.");
			entity.Property(e => e.IsRead)
			      .HasDefaultValue(false)
			      .HasComment("Whether the notification was read.");
			entity.Property(e => e.NotifieeId).HasComment("The ID of recipient user of the Notification.");
			entity.Property(e => e.NotifierId).HasComment("The ID of sender user of the Notification.");
			entity.Property(e => e.Type).HasComment("The type of the Notification.");

			entity.HasOne(d => d.FollowRequest)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Note)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Notifiee)
			      .WithMany(p => p.NotificationNotifiees)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.Notifier)
			      .WithMany(p => p.NotificationNotifiers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserGroupInvitation)
			      .WithMany(p => p.Notifications)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}