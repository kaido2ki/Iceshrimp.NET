using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_list_joining")]
[Index("UserListId", Name = "IDX_605472305f26818cc93d1baaa7")]
[Index("UserId", "UserListId", Name = "IDX_90f7da835e4c10aca6853621e1", IsUnique = true)]
[Index("UserId", Name = "IDX_d844bfc6f3f523a05189076efa")]
public class UserListJoining {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserListJoining.
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
	[InverseProperty("UserListJoinings")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserListId")]
	[InverseProperty("UserListJoinings")]
	public virtual UserList UserList { get; set; } = null!;
}