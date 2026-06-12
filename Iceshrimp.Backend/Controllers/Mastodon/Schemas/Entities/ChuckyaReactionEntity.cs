using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class ChuckyaReactionEntity
{
	[J("id")]         public required string        Id        { get; set; }
	[J("name")]       public required string        Name      { get; set; }
	[J("url")]        public required string?       Url       { get; set; }
	[J("static_url")] public required string?       StaticUrl { get; set; }

	[J("account")]    public required AccountEntity Account   { get; set; }
}