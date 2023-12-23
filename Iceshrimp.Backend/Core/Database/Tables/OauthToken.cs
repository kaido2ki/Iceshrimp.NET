using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("oauth_token")]
[Index("Token", Name = "IDX_2cbeb4b389444bcf4379ef4273")]
[Index("Code", Name = "IDX_dc5fe174a8b59025055f0ec136")]
public class OauthToken {
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the OAuth token
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	[Column("appId")] [StringLength(32)] public string AppId { get; set; } = null!;

	[Column("userId")] [StringLength(32)] public string UserId { get; set; } = null!;

	/// <summary>
	///     The auth code for the OAuth token
	/// </summary>
	[Column("code")]
	[StringLength(64)]
	public string Code { get; set; } = null!;

	/// <summary>
	///     The OAuth token
	/// </summary>
	[Column("token")]
	[StringLength(64)]
	public string Token { get; set; } = null!;

	/// <summary>
	///     Whether or not the token has been activated
	/// </summary>
	[Column("active")]
	public bool Active { get; set; }

	/// <summary>
	///     The scopes requested by the OAuth token
	/// </summary>
	[Column("scopes", TypeName = "character varying(64)[]")]
	public List<string> Scopes { get; set; } = null!;

	/// <summary>
	///     The redirect URI of the OAuth token
	/// </summary>
	[Column("redirectUri")]
	[StringLength(512)]
	public string RedirectUri { get; set; } = null!;

	[ForeignKey("AppId")]
	[InverseProperty("OauthTokens")]
	public virtual OauthApp App { get; set; } = null!;

	[ForeignKey("UserId")]
	[InverseProperty("OauthTokens")]
	public virtual User User { get; set; } = null!;
}