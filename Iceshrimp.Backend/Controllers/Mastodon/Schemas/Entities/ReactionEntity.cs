using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class ReactionEntity
{
	[JI]              public required string  NoteId;
	[J("count")]      public required int     Count     { get; set; }
	[J("me")]         public required bool    Me        { get; set; }
	[J("name")]       public required string  Name      { get; set; }
	[J("url")]        public required string? Url       { get; set; }
	[J("static_url")] public required string? StaticUrl { get; set; }

	[J("accounts")]    public List<AccountEntity>? Accounts   { get; set; }
	[J("account_ids")] public List<string>?        AccountIds { get; set; }
}