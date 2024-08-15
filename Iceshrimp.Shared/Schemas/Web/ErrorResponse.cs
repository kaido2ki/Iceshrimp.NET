using System.Text.Json.Serialization;
using System.Xml.Serialization;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

[XmlRoot("Error")]
public class ErrorResponse
{
	[XmlElement("Code")]  public required int    StatusCode { get; set; }
	[XmlElement("Error")] public required string Error      { get; set; }

	[XmlElement("Message")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; set; }

	[XmlElement("Details")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; set; }

	[XmlIgnore]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IDictionary<string, string[]>? Errors { get; set; }

	[XmlElement("Source")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; set; }

	[XmlElement("RequestId")]
	public required string RequestId { get; set; }
}