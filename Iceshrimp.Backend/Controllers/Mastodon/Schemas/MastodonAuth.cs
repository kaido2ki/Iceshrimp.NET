using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JR = System.Text.Json.Serialization.JsonRequiredAttribute;
using JC = System.Text.Json.Serialization.JsonConverterAttribute;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class MastodonAuth {
	public class VerifyCredentialsResponse {
		public required OauthApp App;

		[J("name")]    public string  Name    => App.Name;
		[J("website")] public string? Website => App.Website;

		[J("vapid_key")] public required string? VapidKey { get; set; }
	}

	public class RegisterAppRequest {
		private List<string> _scopes      = ["read"];
		public  List<string> RedirectUris = [];

		[B(Name = "scopes")]
		[J("scopes")]
		[JC(typeof(EnsureArrayConverter))]
		public List<string> Scopes {
			get => _scopes;
			set => _scopes = value.Count == 1
				? value[0].Split(' ').ToList()
				: value;
		}

		[B(Name = "client_name")]
		[J("client_name")]
		[JR]
		public string ClientName { get; set; } = null!;

		[B(Name = "website")] [J("website")] public string? Website { get; set; }

		[B(Name = "redirect_uris")]
		[J("redirect_uris")]
		public string RedirectUrisInternal {
			set => RedirectUris = value.Split('\n').ToList();
			get => string.Join('\n', RedirectUris);
		}
	}

	public class RegisterAppResponse {
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
}