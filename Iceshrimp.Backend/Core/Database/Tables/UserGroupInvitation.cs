using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_invitation")]
[Index(nameof(UserGroupId))]
[Index(nameof(UserId))]
[Index(nameof(UserId), nameof(UserGroupId), IsUnique = true)]
public class UserGroupInvitation
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserGroupInvitation.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The user ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The group ID.
	/// </summary>
	[Column("userGroupId")]
	[StringLength(32)]
	public string UserGroupId { get; set; } = null!;

	[InverseProperty(nameof(Notification.UserGroupInvitation))]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserGroupInvitations))]
	public virtual User User { get; set; } = null!;

	[ForeignKey(nameof(UserGroupId))]
	[InverseProperty(nameof(Tables.UserGroup.UserGroupInvitations))]
	public virtual UserGroup UserGroup { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserGroupInvitation>
	{
		public void Configure(EntityTypeBuilder<UserGroupInvitation> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupInvitation.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup)
			      .WithMany(p => p.UserGroupInvitations)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroupInvitations)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}