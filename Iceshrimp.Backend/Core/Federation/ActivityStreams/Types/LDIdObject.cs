using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class LDIdObject() {
	public LDIdObject(string id) : this() {
		Id = id;
	}

	[J("@id")] public string? Id { get; set; }

	public override string? ToString() {
		return Id;
	}
}

public sealed class LDIdObjectConverter : ASSerializer.ListSingleObjectConverter<LDIdObject>;