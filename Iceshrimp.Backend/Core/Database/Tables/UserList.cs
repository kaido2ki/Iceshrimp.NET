using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("user_list")]
[Index(nameof(UserId))]
public class UserList
{
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

	[InverseProperty(nameof(Antenna.UserList))]
	public virtual ICollection<Antenna> Antennas { get; set; } = new List<Antenna>();

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.UserLists))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(UserListMember.UserList))]
	public virtual ICollection<UserListMember> UserListMembers { get; set; } = new List<UserListMember>();
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<UserList>
	{
		public void Configure(EntityTypeBuilder<UserList> entity)
		{
			entity.Property(e => e.CreatedAt).HasComment("The created date of the UserList.");
			entity.Property(e => e.HideFromHomeTl)
			      .HasDefaultValue(false)
			      .HasComment("Whether posts from list members should be hidden from the home timeline.");
			entity.Property(e => e.Name).HasComment("The name of the UserList.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");

			entity.HasOne(d => d.User)
			      .WithMany(p => p.UserLists)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}