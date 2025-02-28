﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Iceshrimp.Backend.Core.Database.Tables;

[Table("oauth_app")]
[Index(nameof(ClientId), IsUnique = true)]
public class OauthApp
{
	[Key]
	[Column("id")]
	[StringLength(32)]
	public string Id { get; set; } = null!;

	/// <summary>
	///     The created date of the OAuth application
	/// </summary>
	[Column("createdAt")]
	public DateTime CreatedAt { get; set; }

	/// <summary>
	///     The client id of the OAuth application
	/// </summary>
	[Column("clientId")]
	[StringLength(64)]
	public string ClientId { get; set; } = null!;

	/// <summary>
	///     The client secret of the OAuth application
	/// </summary>
	[Column("clientSecret")]
	[StringLength(64)]
	public string ClientSecret { get; set; } = null!;

	/// <summary>
	///     The name of the OAuth application
	/// </summary>
	[Column("name")]
	[StringLength(128)]
	public string Name { get; set; } = null!;

	/// <summary>
	///     The website of the OAuth application
	/// </summary>
	[Column("website")]
	[StringLength(256)]
	public string? Website { get; set; }

	/// <summary>
	///     The scopes requested by the OAuth application
	/// </summary>
	[Column("scopes", TypeName = "character varying(64)[]")]
	public List<string> Scopes { get; set; } = null!;

	/// <summary>
	///     The redirect URIs of the OAuth application
	/// </summary>
	[Column("redirectUris", TypeName = "character varying(512)[]")]
	public List<string> RedirectUris { get; set; } = null!;

	[InverseProperty(nameof(OauthToken.App))]
	public virtual ICollection<OauthToken> OauthTokens { get; set; } = new List<OauthToken>();

	/// <summary>
	///     The app token, returned by /oauth/token when grant_type client_credentials is requested
	/// </summary>
	[Column("appToken")] [StringLength(64)]
	public string? Token;
	
	private class EntityTypeConfiguration : IEntityTypeConfiguration<OauthApp>
	{
		public void Configure(EntityTypeBuilder<OauthApp> entity)
		{
			entity.Property(e => e.ClientId).HasComment("The client id of the OAuth application");
			entity.Property(e => e.ClientSecret).HasComment("The client secret of the OAuth application");
			entity.Property(e => e.CreatedAt).HasComment("The created date of the OAuth application");
			entity.Property(e => e.Name).HasComment("The name of the OAuth application");
			entity.Property(e => e.RedirectUris).HasComment("The redirect URIs of the OAuth application");
			entity.Property(e => e.Scopes).HasComment("The scopes requested by the OAuth application");
			entity.Property(e => e.Website).HasComment("The website of the OAuth application");			
		}
	}
}