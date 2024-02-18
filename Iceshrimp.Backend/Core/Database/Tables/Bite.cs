﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("bite")]
[Index("Uri")]
[Index("UserId")]
[Index("UserHost")]
[Index("TargetUserId")]
[Index("TargetNoteId")]
[Index("TargetBiteId")]
public class Bite
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")]               public DateTime CreatedAt { get; set; }
	[Column("uri")] [StringLength(512)] public string?  Uri       { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("userHost")]
	[StringLength(512)]
	public string? UserHost { get; set; } = null!;

	[Column("targetUserId")]
	[StringLength(32)]
	public string? TargetUserId { get; set; }

	[Column("targetNoteId")]
	[StringLength(32)]
	public string? TargetNoteId { get; set; }

	[Column("targetBiteId")]
	[StringLength(32)]
	public string? TargetBiteId { get; set; }

	[ForeignKey("UserId")]       public virtual User  User       { get; set; } = null!;
	[ForeignKey("TargetUserId")] public virtual User? TargetUser { get; set; }
	[ForeignKey("TargetNoteId")] public virtual Note? TargetNote { get; set; }
	[ForeignKey("TargetBiteId")] public virtual Bite? TargetBite { get; set; }

	public static string GetIdFromPublicUri(string uri, Config.InstanceSection config) =>
		GetIdFromPublicUri(uri, config.WebDomain);

	public static string GetIdFromPublicUri(string uri, string webDomain) => uri.StartsWith(webDomain)
		? uri["https://{webDomain}/bites/".Length..]
		: throw new Exception("Bite Uri is not local");

	public string GetPublicUri(Config.InstanceSection config) => GetPublicUri(config.WebDomain);

	public string GetPublicUri(string webDomain) => User.Host == null
		? $"https://{webDomain}/bites/{Id}"
		: throw new Exception("Cannot access PublicUri for remote user");
}