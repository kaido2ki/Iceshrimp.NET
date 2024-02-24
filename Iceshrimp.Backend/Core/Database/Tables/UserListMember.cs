using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_list_member")]
[Index("UserListId")]
[Index("UserId", "UserListId", IsUnique = true)]
[Index("UserId")]
public class UserListMember : IEntity
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

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.UserListMembers))]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserListId")]
	[InverseProperty(nameof(Tables.UserList.UserListMembers))]
	public virtual UserList UserList { get; set; } = null!;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;
}