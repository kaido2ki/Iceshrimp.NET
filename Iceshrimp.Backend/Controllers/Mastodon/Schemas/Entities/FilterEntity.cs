using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;

public class FilterEntity
{
	[J("id")]            public required string              Id           { get; set; }
	[J("title")]         public required string              Title        { get; set; }
	[J("context")]       public required List<string>        Context      { get; set; }
	[J("expires_at")]    public required string?             ExpiresAt    { get; set; }
	[J("filter_action")] public required string              FilterAction { get; set; }
	[J("keywords")]      public required List<FilterKeyword> Keywords     { get; set; }

	[J("statuses")] public object[] Statuses => []; //TODO
}

public class FilterKeyword
{
	public FilterKeyword(string keyword, long filterId, int keywordId)
	{
		Id        = $"{filterId}-{keywordId}";
		WholeWord = keyword.StartsWith('"') && keyword.EndsWith('"') && keyword.Length > 2;
		Keyword   = WholeWord ? keyword[1..^1] : keyword;
	}

	[J("id")]         public string Id        { get; }
	[J("keyword")]    public string Keyword   { get; }
	[J("whole_word")] public bool   WholeWord { get; }
}