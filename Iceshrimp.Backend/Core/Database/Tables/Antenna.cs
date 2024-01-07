﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("antenna")]
[Index("UserId")]
public class Antenna {
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
	public string Keywords { get; set; } = null!;

	[Column("withFile")] public bool WithFile { get; set; }

	[Column("expression")]
	[StringLength(2048)]
	public string? Expression { get; set; }

	[Column("notify")] public bool Notify { get; set; }

	[Column("caseSensitive")] public bool CaseSensitive { get; set; }

	[Column("withReplies")] public bool WithReplies { get; set; }

	[Column("UserGroupMemberId")]
	[StringLength(32)]
	public string? UserGroupMemberId { get; set; }

	[Column("users", TypeName = "character varying(1024)[]")]
	public List<string> Users { get; set; } = null!;

	[Column("excludeKeywords", TypeName = "jsonb")]
	public string ExcludeKeywords { get; set; } = null!;

	[Column("instances", TypeName = "jsonb")]
	public string Instances { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("Antennas")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("UserGroupMemberId")]
	[InverseProperty("Antennas")]
	public virtual UserGroupMember? UserGroupMember { get; set; }

	[ForeignKey("UserListId")]
	[InverseProperty("Antennas")]
	public virtual UserList? UserList { get; set; }

	[PgName("antenna_src_enum")]
	public enum AntennaSource {
		[PgName("home")]      Home,
		[PgName("all")]       All,
		[PgName("users")]     Users,
		[PgName("list")]      List,
		[PgName("group")]     Group,
		[PgName("instances")] Instances
	}
}