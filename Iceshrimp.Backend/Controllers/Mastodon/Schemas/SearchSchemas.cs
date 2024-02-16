using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Microsoft.AspNetCore.Mvc;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public abstract class SearchSchemas {
	public class SearchRequest {
		[FromQuery(Name = "q")]          public string? Query     { get; set; }
		[FromQuery(Name = "type")]       public string? Type      { get; set; }
		[FromQuery(Name = "resolve")]    public bool    Resolve   { get; set; } = false;
		[FromQuery(Name = "following")]  public bool    Following { get; set; } = false;
		[FromQuery(Name = "account_id")] public string? UserId    { get; set; }

		[FromQuery(Name = "exclude_unreviewed")]
		public bool ExcludeUnreviewed { get; set; } = false;
	}

	public class SearchResponse {
		[J("accounts")] public required List<AccountEntity> Accounts { get; set; }
		[J("statuses")] public required List<StatusEntity>  Statuses { get; set; }
		[J("hashtags")] public          List<object>  Hashtags => []; //TODO: implement this
	}
}