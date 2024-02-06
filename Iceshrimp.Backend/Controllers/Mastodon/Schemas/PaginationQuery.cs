using Microsoft.AspNetCore.Mvc;
using B = Microsoft.AspNetCore.Mvc.BindPropertyAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class PaginationQuery {
	[FromQuery] [B(Name = "max_id")]   public string? MaxId   { get; set; }
	[FromQuery] [B(Name = "since_id")] public string? SinceId { get; set; }
	[FromQuery] [B(Name = "min_id")]   public string? MinId   { get; set; }
	[FromQuery] [B(Name = "limit")]    public int?    Limit   { get; set; }
}