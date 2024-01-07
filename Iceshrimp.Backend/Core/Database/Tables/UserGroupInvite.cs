using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_invite")]
[Index("UserId")]
[Index("UserId", "UserGroupId", IsUnique = true)]
[Index("UserGroupId")]
public class UserGroupInvite {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("userGroupId")]
	[StringLength(32)]
	public string UserGroupId { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("UserGroupInvites")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserGroupId")]
	[InverseProperty("UserGroupInvites")]
	public virtual UserGroup UserGroup { get; set; } = null!;
}