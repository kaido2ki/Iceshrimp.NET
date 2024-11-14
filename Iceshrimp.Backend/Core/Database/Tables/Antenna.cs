using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("antenna")]
[Index(nameof(UserId))]
public class Antenna
{
	[PgName("antenna_src_enum")]
	public enum AntennaSource
	{
		[PgName("home")]      Home,
		[PgName("all")]       All,
		[PgName("users")]     Users,
		[PgName("list")]      List,
		[PgName("group")]     Group,
		[PgName("instances")] Instances
	}

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the Antenna.
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
	///     The name of the Antenna.
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	[Column("src")] public AntennaSource Source { get; set; }

	[Column("userListId")]
	[StringLength(32)]
	public string? UserListId { get; set; }

	[Column("keywords", TypeName = "jsonb")]
	public List<List<string>> Keywords { get; set; } = [];

	[Column("withFile")] public bool WithFile { get; set; }

	[Column("expression")]
	[StringLength(2048)]
	public string? Expression { get; set; }

	[Column("notify")] public bool Notify { get; set; }

	[Column("caseSensitive")] public bool CaseSensitive { get; set; }

	[Column("withReplies")] public bool WithReplies { get; set; }

	[Column("userGroupMemberId")]
	[StringLength(32)]
	public string? UserGroupMemberId { get; set; }

	[Column("users", TypeName = "character varying(1024)[]")]
	public List<string> Users { get; set; } = null!;

	[Column("excludeKeywords", TypeName = "jsonb")]
	public List<List<string>> ExcludeKeywords { get; set; } = [];

	//TODO: refactor this column (this should have been a varchar[]) 
	[Column("instances", TypeName = "jsonb")]
	public List<string> Instances { get; set; } = [];

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.Antennas))]
	public virtual User User { get; set; } = null!;

	[ForeignKey(nameof(UserGroupMemberId))]
	[InverseProperty(nameof(Tables.UserGroupMember.Antennas))]
	public virtual UserGroupMember? UserGroupMember { get; set; }

	[ForeignKey(nameof(UserListId))]
	[InverseProperty(nameof(Tables.UserList.Antennas))]
	public virtual UserList? UserList { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<Antenna>
	{
		public void Configure(EntityTypeBuilder<Antenna> entity)
		{
			entity.Property(e => e.CaseSensitive).HasDefaultValue(false);
			entity.Property(e => e.CreatedAt).HasComment("The created date of the Antenna.");
			entity.Property(e => e.ExcludeKeywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Instances).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Keywords).HasDefaultValueSql("'[]'::jsonb");
			entity.Property(e => e.Name).HasComment("The name of the Antenna.");
			entity.Property(e => e.UserId).HasComment("The owner ID.");
			entity.Property(e => e.Users).HasDefaultValueSql("'{}'::character varying[]");
			entity.Property(e => e.WithReplies).HasDefaultValue(false);

			entity.HasOne(d => d.UserGroupMember)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.UserList)
			      .WithMany(p => p.Antennas)
			      .OnDelete(DeleteBehavior.Cascade);			
		}
	}
}