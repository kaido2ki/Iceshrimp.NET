using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

//TODO: handle object | link correctly (we don't need to resolve it if it's already the full object)
public class ASIdObject() {
	public ASIdObject(string id) : this() {
		Id = id;
	}

	[J("@id")] public string? Id { get; set; }

	public override string? ToString() {
		return Id;
	}
}

public sealed class LDIdObjectConverter : ASSerializer.ListSingleObjectConverter<ASIdObject>;