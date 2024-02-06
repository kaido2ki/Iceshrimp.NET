using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_list")]
[Index("UserId")]
public class UserList {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the UserList.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The owner ID.
	/// </summary>
	[Column("userId")]
	[StringLength(32)]
	public string UserId { get; set; } = null!;

	/// <summary>
	///     The name of the UserList.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     Whether posts from list members should be hidden from the home timeline.
	/// </summary>
	[Column("hideFromHomeTl")]
	public bool HideFromHomeTl { get; set; }

	[InverseProperty(nameof(Antenna.UserList))] public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.UserLists))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(UserListMember.UserList))]
	public virtual ICollection<UserListMember> UserListMembers { get; set; } = new List<UserListMember>();
}