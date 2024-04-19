using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("push_subscription")]
[Index(nameof(UserId))]
[Index("OauthTokenId", IsUnique = true)]
public class PushSubscription
{
	[PgName("push_subscription_policy_enum")]
	public enum PushPolicy
	{
		[PgName("all")]      All,
		[PgName("followed")] Followed,
		[PgName("follower")] Follower,
		[PgName("none")]     None
	}

	[Column("types", TypeName = "character varying(32)[]")]
	public List<string> Types = null!;

	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	[Column("createdAt")] public DateTime CreatedAt { get; set; }

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("oauthTokenId")]
	[StringLength(32)]
	public string OauthTokenId { get; set; } = null!;

	[Column("endpoint")]
	[StringLength(512)]
	public string Endpoint { get; set; } = null!;

	[Column("auth")] [StringLength(256)] public string AuthSecret { get; set; } = null!;

	[Column("publickey")]
	[StringLength(128)]
	public string PublicKey { get; set; } = null!;

	[Column("policy")] public PushPolicy Policy { get; set; }

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.PushSubscriptions))]
	public virtual User User { get; set; } = null!;

	[ForeignKey("OauthTokenId")]
	[InverseProperty(nameof(Tables.OauthToken.PushSubscription))]
	public virtual OauthToken OauthToken { get; set; } = null!;
}