using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("hashtag")]
[Index("AttachedRemoteUsersCount", Name = "IDX_0b03cbcd7e6a7ce068efa8ecc2")]
[Index("AttachedLocalUsersCount", Name = "IDX_0c44bf4f680964145f2a68a341")]
[Index("MentionedLocalUsersCount", Name = "IDX_0e206cec573f1edff4a3062923")]
[Index("MentionedUsersCount", Name = "IDX_2710a55f826ee236ea1a62698f")]
[Index("Name", Name = "IDX_347fec870eafea7b26c8a73bac", IsUnique = true)]
[Index("MentionedRemoteUsersCount", Name = "IDX_4c02d38a976c3ae132228c6fce")]
[Index("AttachedUsersCount", Name = "IDX_d57f9030cd3af7f63ffb1c267c")]
public class Hashtag {
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