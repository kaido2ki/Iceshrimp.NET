using System.ComponentModel.DataAnnotations;

namespace Iceshrimp.Shared.Schemas.Web;

public class FilterRequest
{
	[MinLength(1)] public required string Name { get; init; }

	public required DateTime?                          Expiry   { get; init; }
	public required List<string>                       Keywords { get; init; }
	public required FilterResponse.FilterAction        Action   { get; init; }
	public required List<FilterResponse.FilterContext> Contexts { get; init; }
}