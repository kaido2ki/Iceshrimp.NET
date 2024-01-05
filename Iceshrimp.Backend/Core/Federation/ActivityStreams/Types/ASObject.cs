using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASObject {
	[J("@id")]   public string?       Id   { get; set; }
	[J("@type")] public List<string>? Type { get; set; } //TODO: does this really need to be a list?

	//FIXME: don't recurse creates and co
	public static ASObject? Deserialize(JToken token) {
		return token.Type switch {
			JTokenType.Object => token["@type"]?[0]?.Value<string>() switch {
				"https://www.w3.org/ns/activitystreams#Application"  => token.ToObject<ASActor>(),
				"https://www.w3.org/ns/activitystreams#Group"        => token.ToObject<ASActor>(),
				"https://www.w3.org/ns/activitystreams#Organization" => token.ToObject<ASActor>(),
				"https://www.w3.org/ns/activitystreams#Person"       => token.ToObject<ASActor>(),
				"https://www.w3.org/ns/activitystreams#Service"      => token.ToObject<ASActor>(),
				"https://www.w3.org/ns/activitystreams#Note"         => token.ToObject<ASNote>(),
				"https://www.w3.org/ns/activitystreams#Create"       => token.ToObject<ASActivity>(),
				_                                                    => null
			},
			JTokenType.String => new ASObject { Id = token.Value<string>() },
			_                 => throw new ArgumentOutOfRangeException()
		};
	}
}