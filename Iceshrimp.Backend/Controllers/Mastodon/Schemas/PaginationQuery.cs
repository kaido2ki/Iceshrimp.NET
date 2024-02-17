using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class PaginationQuery
{
	[FromQuery(Name = "max_id")]   public string? MaxId   { get; set; }
	[FromQuery(Name = "since_id")] public string? SinceId { get; set; }
	[FromQuery(Name = "min_id")]   public string? MinId   { get; set; }
	[FromQuery(Name = "limit")]    public int?    Limit   { get; set; }
	[FromQuery(Name = "offset")]   public int?    Offset  { get; set; }
}