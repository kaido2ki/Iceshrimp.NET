using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class Mention {
	[J("id")]       public required string Id       { get; set; }
	[J("username")] public required string Username { get; set; }
	[J("acct")]     public required string Acct     { get; set; }
	[J("url")]      public required string Url      { get; set; }
}