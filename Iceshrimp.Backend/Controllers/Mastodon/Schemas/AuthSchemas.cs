using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using JC = System.Text.Json.Serialization.JsonConverterAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class AuthSchemas
{
	public class VerifyAppCredentialsResponse
	{
		public required OauthApp App;

		[J("name")]    public string  Name    => App.Name;
		[J("website")] public string? Website => App.Website;

		[J("vapid_key")] public required string? VapidKey { get; set; }
	}

	public class RegisterAppRequest
	{
		private List<string> _scopes      = ["read"];
		public  List<string> RedirectUris = [];

		[B(Name = "scopes")]
		[J("scopes")]
		[JC(typeof(EnsureArrayConverter))]
		public List<string> Scopes
		{
			get => _scopes;
			set => _scopes = value.Count == 1
				? value[0].Contains(' ')
					? value[0].Split(' ').ToList()
					: value[0].Split(',').ToList()
				: value;
		}

		[B(Name = "client_name")]
		[J("client_name")]
		[JR]
		public string ClientName { get; set; } = null!;

		[B(Name = "website")] [J("website")] public string? Website { get; set; }

		[B(Name = "redirect_uris")]
		[J("redirect_uris")]
		public string RedirectUrisInternal
		{
			set => RedirectUris = value.Split('\n').ToList();
			get => string.Join('\n', RedirectUris);
		}
	}

	public class RegisterAppResponse
	{
		public required OauthApp App;

		[J("id")]            public string       Id           => App.Id;
		[J("name")]          public string       Name         => App.Name;
		[J("website")]       public string?      Website      => App.Website;
		[J("scopes")]        public List<string> Scopes       => App.Scopes;
		[J("redirect_uri")]  public string       RedirectUri  => string.Join("\n", App.RedirectUris);
		[J("client_id")]     public string       ClientId     => App.ClientId;
		[J("client_secret")] public string       ClientSecret => App.ClientSecret;

		[J("vapid_key")] public required string? VapidKey { get; set; }
	}

	public class OauthTokenRequest
	{
		public List<string>? Scopes;

		[B(Name = "scope")]
		[J("scope")]
		[JC(typeof(EnsureArrayConverter))]
		public List<string> ScopesInternal
		{
			get => Scopes ?? [];
			set => Scopes = value.Count == 1
				? value[0].Contains(' ')
					? value[0].Split(' ').ToList()
					: value[0].Split(',').ToList()
				: value;
		}

		[B(Name = "redirect_uri")]
		[J("redirect_uri")]
		[JR]
		public string RedirectUri { get; set; } = null!;

		[B(Name = "grant_type")]
		[J("grant_type")]
		[JR]
		public string GrantType { get; set; } = null!;

		[B(Name = "client_id")]
		[J("client_id")]
		[JR]
		public string ClientId { get; set; } = null!;

		[B(Name = "client_secret")]
		[J("client_secret")]
		[JR]
		public string ClientSecret { get; set; } = null!;

		[B(Name = "code")] [J("code")] public string? Code { get; set; } = null!;
	}

	public class OauthTokenResponse
	{
		public required DateTime CreatedAt;

		public required                     List<string> Scopes;
		[J("access_token")] public required string       AccessToken { get; set; }

		[J("token_type")] public string TokenType         => "Bearer";
		[J("scope")]      public string Scope             => string.Join(' ', Scopes);
		[J("created_at")] public long   CreatedAtInternal => (long)(CreatedAt - DateTime.UnixEpoch).TotalSeconds;
	}

	public class OauthTokenRevocationRequest
	{
		[B(Name = "client_id")]
		[J("client_id")]
		[JR]
		public string ClientId { get; set; } = null!;

		[B(Name = "client_secret")]
		[J("client_secret")]
		[JR]
		public string ClientSecret { get; set; } = null!;

		[B(Name = "code")] [J("token")] [JR] public string Token { get; set; } = null!;
	}
}