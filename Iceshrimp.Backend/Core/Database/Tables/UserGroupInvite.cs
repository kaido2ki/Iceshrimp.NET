using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_invite")]
[Index("UserId", Name = "IDX_1039988afa3bf991185b277fe0")]
[Index("UserId", "UserGroupId", Name = "IDX_78787741f9010886796f2320a4", IsUnique = true)]
[Index("UserGroupId", Name = "IDX_e10924607d058004304611a436")]
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