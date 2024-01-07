using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_member")]
[Index("UserGroupId", Name = "IDX_67dc758bc0566985d1b3d39986")]
[Index("UserId", "UserGroupId", Name = "IDX_d9ecaed8c6dc43f3592c229282", IsUnique = true)]
[Index("UserId", Name = "IDX_f3a1b4bd0c7cabba958a0c0b23")]
public class UserGroupMember {
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

	[InverseProperty("UserGroupMember")]
	public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[ForeignKey("UserId")]
	[InverseProperty("UserGroupMembers")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserGroupId")]
	[InverseProperty("UserGroupMembers")]
	public virtual UserGroup UserGroup { get; set; } = null!;
}