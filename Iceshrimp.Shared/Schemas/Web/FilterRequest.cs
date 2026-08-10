using System.ComponentModel.DataAnnotations;

namespace Iceshrimp.Shared.Schemas.Web;

public class FilterRequest
{
	[MinLength(1)] public required string Name { get; set; }

	public required DateTime?                          Expiry   { get; set; }
	public required List<string>                       Keywords { get; set; }
	public required FilterResponse.FilterAction        Action   { get; set; }
	public required List<FilterResponse.FilterContext> Contexts { get; set; }
}