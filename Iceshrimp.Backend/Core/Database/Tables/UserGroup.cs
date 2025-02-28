﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group")]
[Index(nameof(CreatedAt))]
[Index(nameof(UserId))]
public class UserGroup
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserGroup.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("name")] [StringLength(256)] public string Name { get; set; } = null!;

	/// <summary>
	///     The ID of owner.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	[Column("isPrivate")] public bool IsPrivate { get; set; }

	[InverseProperty(nameof(MessagingMessage.Group))]
	public virtual ICollection<MessagingMessage> MessagingMessages { get; set; } = new List<MessagingMessage>();

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserGroups))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(UserGroupInvitation.UserGroup))]
	public virtual ICollection<UserGroupInvitation> UserGroupInvitations { get; set; } =
		new List<UserGroupInvitation>();

	[InverseProperty(nameof(UserGroupMember.UserGroup))]
	public virtual ICollection<UserGroupMember> UserGroupMembers { get; set; } = new List<UserGroupMember>();

	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserGroup>
	{
		public void Configure(EntityTypeBuilder<UserGroup> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroup.");
			entity.Property(e => e.IsPrivate).HasDefaultValue(false);
			entity.Property(e => e.UserId).HasComment("The ID of owner.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroups)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}