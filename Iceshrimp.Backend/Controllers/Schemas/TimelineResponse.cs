using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Schemas;

public class TimelineResponse
{
	[J("notes")] public required IEnumerable<NoteResponse> Notes { get; set; }
	[J("limit")] public required int                       Limit { get; set; }
}