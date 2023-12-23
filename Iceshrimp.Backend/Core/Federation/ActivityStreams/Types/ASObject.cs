using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASObject {
	[J("@id")]   public string?       Id   { get; set; }
	[J("@type")] public List<string>? Type { get; set; } //TODO: does this really need to be a list?
}