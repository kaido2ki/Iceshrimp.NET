using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class ErrorResponse
{
	[J("statusCode")] public required int    StatusCode { get; set; }
	[J("error")]      public required string Error      { get; set; }

	[J("message")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; set; }

	[J("details")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; set; }

	[J("source")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; set; }

	[J("requestId")] public required string RequestId { get; set; }
}