using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Shared.Schemas;

public class PaginationQuery : IPaginationQuery
{
	[FromQuery(Name = "max_id")] public string? MaxId { get; set; }
	[FromQuery(Name = "min_id")] public string? MinId { get; set; }
	[FromQuery(Name = "limit")]  public int?    Limit { get; set; }
}

// TODO: replace PaginationQuery with this once all endpoints are converted over
public class PaginationQueryCursor : IPaginationQueryCursor
{
	[FromQuery(Name = "cursor")] public string? Cursor { get; set; }
	[FromQuery(Name = "limit")]  public int?    Limit  { get; set; }
}
