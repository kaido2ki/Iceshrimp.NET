using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class ErrorResponse {
	[J("statusCode")] public required int    StatusCode { get; set; }
	[J("error")]      public required string Error      { get; set; }
	[J("message")]    public required string Message    { get; set; }
	[J("requestId")]  public required string RequestId  { get; set; }
}