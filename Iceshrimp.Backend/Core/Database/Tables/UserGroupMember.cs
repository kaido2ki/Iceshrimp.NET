using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_group_member")]
[Index("UserGroupId")]
[Index("UserId", "UserGroupId", IsUnique = true)]
[Index("UserId")]
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
	[InverseProperty("UserGroupMemberships")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserGroupId")]
	[InverseProperty("UserGroupMembers")]
	public virtual UserGroup UserGroup { get; set; } = null!;
}