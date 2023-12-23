using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_invitation")]
[Index("UserGroupId", Name = "IDX_5cc8c468090e129857e9fecce5")]
[Index("UserId", Name = "IDX_bfbc6305547539369fe73eb144")]
[Index("UserId", "UserGroupId", Name = "IDX_e9793f65f504e5a31fbaedbf2f", IsUnique = true)]
public class UserGroupInvitation {
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

	[InverseProperty("UserGroupInvitation")]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[ForeignKey("UserId")]
	[InverseProperty("UserGroupInvitations")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserGroupId")]
	[InverseProperty("UserGroupInvitations")]
	public virtual UserGroup UserGroup { get; set; } = null!;
}