using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("access_token")]
[Index("Hash")]
[Index("Token")]
[Index("UserId")]
[Index("Session")]
public class AccessToken {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the AccessToken.
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("token")] [StringLength(128)] public string Token { get; set; } = null!;

	[Column("hash")] [StringLength(128)] public string Hash { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	[Column("appId")] [StringLength(32)] public string? AppId { get; set; }

	[Column("lastUsedAt")] public DateTime? LastUsedAt { get; set; }

	[Column("session")]
	[StringLength(128)]
	public string? Session { get; set; }

	[Column("name")] [StringLength(128)] public string? Name { get; set; }

	[Column("description")]
	[StringLength(512)]
	public string? Description { get; set; }

	[Column("iconUrl")]
	[StringLength(512)]
	public string? IconUrl { get; set; }

	[Column("permission", TypeName = "character varying(64)[]")]
	public List<string> Permission { get; set; } = null!;

	[Column("fetched")] public bool Fetched { get; set; }

	[ForeignKey("AppId")]
	[InverseProperty(nameof(Tables.App.AccessTokens))]
	public virtual App? App { get; set; }

	[InverseProperty(nameof(Notification.AppAccessToken))]
	public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

	[ForeignKey("UserId")]
	[InverseProperty(nameof(Tables.User.AccessTokens))]
	public virtual User User { get; set; } = null!;
}