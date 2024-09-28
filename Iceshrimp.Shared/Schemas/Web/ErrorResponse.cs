using System.Text.Json.Serialization;
using System.Xml.Serialization;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Iceshrimp.Shared.Schemas.Web;

[XmlRoot("Error")]
public class ErrorResponse()
{
	public ErrorResponse(Exception exception) : this()
	{
		Exception = exception;
	}

	[XmlElement("Status")] public required int    StatusCode { get; set; }
	[XmlElement("Error")]  public required string Error      { get; set; }

	[XmlElement("Message")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Message { get; set; }

	[XmlElement("Details")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Details { get; set; }

	[XmlIgnore]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IDictionary<string, string[]>? Errors { get; set; }

	[XmlArray("ValidationErrors")]
	[XmlArrayItem("Error")]
	[JI]
	public XmlValidationError[]? XmlErrors
	{
		get => Errors?.SelectMany(x => x.Value.Select(i => new XmlValidationError { Element = x.Key, Error = i }))
		             .ToArray();
		set => Errors = value?.ToDictionary(p => p.Element, p => (string[]) [p.Error]);
	}

	[XmlElement("Source")]
	[JI(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; set; }

	[XmlElement("RequestId")] public required string RequestId { get; set; }

	[JI] [XmlIgnore] public Exception Exception = new();
}

public class XmlValidationError
{
	[XmlAttribute("Element")] public required string Element { get; set; }
	[XmlAttribute("Error")]   public required string Error   { get; set; }
}