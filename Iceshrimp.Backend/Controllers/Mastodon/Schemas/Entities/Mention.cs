using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database.Tables;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class Mention() {
	[J("id")]       public required string Id       { get; set; }
	[J("username")] public required string Username { get; set; }
	[J("acct")]     public required string Acct     { get; set; }
	[J("url")]      public required string Url      { get; set; }

	// internal properties that won't be serialized
	[JI] public required string  Uri;
	[JI] public required string? Host;

	[SetsRequiredMembers]
	public Mention(User u, string webDomain) : this() {
		Id       = u.Id;
		Username = u.Username;
		Host     = u.Host;
		Acct     = u.Acct;
		Uri      = u.Uri ?? $"https://{webDomain}/users/{u.Id}";
		Url = u.UserProfile != null
			? u.UserProfile.Url ?? u.Uri ?? $"https://{webDomain}/@{u.Username}"
			: u.Uri ?? $"https://{webDomain}/@{u.Username}";
	}
}