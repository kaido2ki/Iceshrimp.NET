using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class LDIdObject(string id) {
	[J("@id")] public string? Id { get; set; } = id;

	public override string? ToString() {
		return Id;
	}
}

public sealed class LDIdObjectConverter : ASSerializer.ListSingleObjectConverter<ASLink>;