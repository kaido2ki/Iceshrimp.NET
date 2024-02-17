using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("hashtag")]
[Index("AttachedRemoteUsersCount")]
[Index("AttachedLocalUsersCount")]
[Index("MentionedLocalUsersCount")]
[Index("MentionedUsersCount")]
[Index("Name", IsUnique = true)]
[Index("MentionedRemoteUsersCount")]
[Index("AttachedUsersCount")]
public class Hashtag
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("name")] [StringLength(128)] public string Name { get; set; } = null!;

	[Column("mentionedUserIds", TypeName = "character varying(32)[]")]
	public List<string> MentionedUserIds { get; set; } = null!;

	[Column("mentionedUsersCount")] public int MentionedUsersCount { get; set; }

	[Column("mentionedLocalUserIds", TypeName = "character varying(32)[]")]
	public List<string> MentionedLocalUserIds { get; set; } = null!;

	[Column("mentionedLocalUsersCount")] public int MentionedLocalUsersCount { get; set; }

	[Column("mentionedRemoteUserIds", TypeName = "character varying(32)[]")]
	public List<string> MentionedRemoteUserIds { get; set; } = null!;

	[Column("mentionedRemoteUsersCount")] public int MentionedRemoteUsersCount { get; set; }

	[Column("attachedUserIds", TypeName = "character varying(32)[]")]
	public List<string> AttachedUserIds { get; set; } = null!;

	[Column("attachedUsersCount")] public int AttachedUsersCount { get; set; }

	[Column("attachedLocalUserIds", TypeName = "character varying(32)[]")]
	public List<string> AttachedLocalUserIds { get; set; } = null!;

	[Column("attachedLocalUsersCount")] public int AttachedLocalUsersCount { get; set; }

	[Column("attachedRemoteUserIds", TypeName = "character varying(32)[]")]
	public List<string> AttachedRemoteUserIds { get; set; } = null!;

	[Column("attachedRemoteUsersCount")] public int AttachedRemoteUsersCount { get; set; }
}