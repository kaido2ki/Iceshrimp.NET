using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_member")]
[Index(nameof(UserGroupId))]
[Index(nameof(UserId), nameof(UserGroupId), IsUnique = true)]
[Index(nameof(UserId))]
public class UserGroupMember
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserGroupMember.
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

	[InverseProperty(nameof(Antenna.UserGroupMember))]
	public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserGroupMemberships))]
	public virtual User User { get; set; } = null!;

	[ForeignKey(nameof(UserGroupId))]
	[InverseProperty(nameof(Tables.UserGroup.UserGroupMembers))]
	public virtual UserGroup UserGroup { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserGroupMember>
	{
		public void Configure(EntityTypeBuilder<UserGroupMember> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserGroupMember.");
			entity.Property(e => e.UserGroupId).HasComment("The group ID.");
			entity.Property(e => e.UserId).HasComment("The user ID.");

			entity.HasOne(d => d.UserGroup)
			      .WithMany(p => p.UserGroupMembers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserGroupMemberships)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}