using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas.Entities;

public class AkkomaTranslationEntity
{
	[J("text")]              public required string Text             { get; set; }
	[J("detected_language")] public required string DetectedLanguage { get; set; }
}