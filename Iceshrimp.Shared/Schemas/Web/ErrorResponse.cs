using System.Text.Json.Serialization;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

public class ErrorResponse
{
	public required int    StatusCode { get; set; }
	public required string Error      { get; set; }

	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; set; }

	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; set; }

	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; set; }

	public required string RequestId { get; set; }
}