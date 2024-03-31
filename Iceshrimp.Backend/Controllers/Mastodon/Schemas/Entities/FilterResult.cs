using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class FilterResultEntity
{
	[J("filter")]          public required FilterEntity Filter         { get; set; }
	[J("keyword_matches")] public required List<string> KeywordMatches { get; set; }

	[J("status_matches")] public List<string> StatusMatches => []; //TODO
}