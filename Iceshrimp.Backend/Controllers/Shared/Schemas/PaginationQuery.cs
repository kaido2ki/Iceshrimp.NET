using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Iceshrimp.Backend.Controllers.Shared.Schemas;

public class PaginationQuery : IPaginationQuery
{
	/// <summary>
	/// Include items that are older than this ID
	/// </summary>
	[FromQuery(Name = "max_id")]
	public string? MaxId { get; set; }

	/// <summary>
	/// Include items that are newer than this ID
	/// </summary>
	[FromQuery(Name = "min_id")]
	public string? MinId { get; set; }

	/// <summary>
	/// Number of items per page
	/// </summary>
	[FromQuery(Name = "limit")]
	public int? Limit { get; set; }
}