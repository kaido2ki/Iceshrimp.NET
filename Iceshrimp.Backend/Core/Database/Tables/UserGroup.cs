using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group")]
[Index("CreatedAt", Name = "IDX_20e30aa35180e317e133d75316")]
[Index("UserId", Name = "IDX_3d6b372788ab01be58853003c9")]
public class UserGroup {
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

	[InverseProperty("Group")]
	public virtual ICollection<MessagingMessage> MessagingMessages { get; set; } = new List<MessagingMessage>();

	[ForeignKey("UserId")]
	[InverseProperty("UserGroups")]
	public virtual User User { get; set; } = null!;

	[InverseProperty("UserGroup")]
	public virtual ICollection<UserGroupInvitation> UserGroupInvitations { get; set; } =
		new List<UserGroupInvitation>();

	[InverseProperty("UserGroup")]
	public virtual ICollection<UserGroupInvite> UserGroupInvites { get; set; } = new List<UserGroupInvite>();

	[InverseProperty("UserGroup")]
	public virtual ICollection<UserGroupJoining> UserGroupJoinings { get; set; } = new List<UserGroupJoining>();
}