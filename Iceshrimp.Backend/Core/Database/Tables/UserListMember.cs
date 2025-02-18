using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Iceshrimp.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_list_member")]
[Index(nameof(UserListId))]
[Index(nameof(UserId), nameof(UserListId), IsUnique = true)]
[Index(nameof(UserId))]
public class UserListMember : IIdentifiable
{
	/// <summary>
	///     The created date of the UserListMember.
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
	///     The list ID.
	/// </summary>
	[Column("userListId")]
	[StringLength(32)]
	public string UserListId { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserListMembers))]
	public virtual User User { get; set; } = null!;

	[ForeignKey(nameof(UserListId))]
	[InverseProperty(nameof(Tables.UserList.UserListMembers))]
	public virtual UserList UserList { get; set; } = null!;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserListMember>
	{
		public void Configure(EntityTypeBuilder<UserListMember> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserListMember.");
			entity.Property(e => e.UserId).HasComment("The user ID.");
			entity.Property(e => e.UserListId).HasComment("The list ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserListMembers)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserList)
			      .WithMany(p => p.UserListMembers)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}