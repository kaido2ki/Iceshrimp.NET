using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("oauth_token")]
[Index(nameof(Token))]
[Index(nameof(Code))]
public class OauthToken
{
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

	[ForeignKey(nameof(AppId))]
	[InverseProperty(nameof(OauthApp.OauthTokens))]
	public virtual OauthApp App { get; set; } = null!;

	[ForeignKey(nameof(UserId))]
	[InverseProperty(nameof(Tables.User.OauthTokens))]
	public virtual User User { get; set; } = null!;

	[InverseProperty(nameof(Tables.PushSubscription.OauthToken))]
	public virtual PushSubscription? PushSubscription { get; set; }

	[Column("supportsHtmlFormatting")] public bool SupportsHtmlFormatting { get; set; }
	[Column("autoDetectQuotes")]       public bool AutoDetectQuotes       { get; set; }
	[Column("isPleroma")]              public bool IsPleroma              { get; set; }

	[Column("lastActiveDate")] public DateTime? LastActiveDate { get; set; }
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<OauthToken>
	{
		public void Configure(EntityTypeBuilder<OauthToken> entity)
		{
			entity.Property(e => e.Active).HasComment("Whether or not the token has been activated");
			entity.Property(e => e.Code).HasComment("The auth code for the OAuth token");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth token");
			entity.Property(e => e.RedirectUri).HasComment("The redirect URI of the OAuth token");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth token");
			entity.Property(e => e.Token).HasComment("The OAuth token");
			entity.Property(e => e.SupportsHtmlFormatting)
			      .HasComment("Whether the client supports HTML inline formatting (bold, italic, strikethrough, ...)")
			      .HasDefaultValue(true);
			entity.Property(e => e.AutoDetectQuotes)
			      .HasComment("Whether the backend should automatically detect quote posts coming from this client")
			      .HasDefaultValue(true);
			entity.Property(e => e.IsPleroma)
			      .HasComment("Whether Pleroma or Akkoma specific behavior should be enabled for this client")
			      .HasDefaultValue(false);

			entity.HasOne(d => d.App)
			      .WithMany(p => p.OauthTokens)
			      .OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(d => d.User)
			      .WithMany(p => p.OauthTokens)
			      .OnDelete(DeleteBehavior.Cascade);
		}
	}
}