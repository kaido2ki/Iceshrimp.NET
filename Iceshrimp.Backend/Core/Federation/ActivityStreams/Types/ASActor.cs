using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActor : ASObject {
	[J("https://misskey-hub.net/ns#_misskey_summary")]
	[JC(typeof(VC))]
	public string? MkSummary { get; set; }

	[J("https://www.w3.org/ns/activitystreams#summary")]
	[JC(typeof(VC))]
	public string? Summary { get; set; }

	[J("https://www.w3.org/ns/activitystreams#name")]
	[JC(typeof(VC))]
	public string? DisplayName { get; set; }

	[J("https://www.w3.org/ns/activitystreams#preferredUsername")]
	[JC(typeof(VC))]
	public string? Username { get; set; }

	[J("http://joinmastodon.org/ns#discoverable")]
	[JC(typeof(VC))]
	public bool? IsDiscoverable { get; set; }

	[J("http://joinmastodon.org/ns#indexable")]
	[JC(typeof(VC))]
	public bool? IsIndexable { get; set; }

	[J("http://joinmastodon.org/ns#memorial")]
	[JC(typeof(VC))]
	public bool? IsMemorial { get; set; }

	[J("https://www.w3.org/ns/activitystreams#manuallyApprovesFollowers")]
	[JC(typeof(VC))]
	public bool? IsLocked { get; set; }

	[J("https://misskey-hub.net/ns#isCat")]
	[JC(typeof(VC))]
	public bool? IsCat { get; set; }

	[J("http://www.w3.org/2006/vcard/ns#Address")]
	[JC(typeof(VC))]
	public string? Location { get; set; }

	[J("http://www.w3.org/2006/vcard/ns#bday")]
	[JC(typeof(VC))]
	public string? Birthday { get; set; }

	[J("https://www.w3.org/ns/activitystreams#endpoints")]
	[JC(typeof(ASEndpointsConverter))]
	public ASEndpoints? Endpoints { get; set; }

	[J("https://www.w3.org/ns/activitystreams#outbox")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection? Outbox { get; set; }

	[J("http://www.w3.org/ns/ldp#inbox")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Inbox { get; set; }

	[J("https://www.w3.org/ns/activitystreams#followers")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection? Followers { get; set; } //FIXME: <ASActor>

	[J("https://www.w3.org/ns/activitystreams#following")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection? Following { get; set; } //FIXME: <ASActor>

	[J("https://www.w3.org/ns/activitystreams#sharedInbox")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? SharedInbox { get; set; }

	[J("https://www.w3.org/ns/activitystreams#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	public bool? IsBot => Type?.Any(p => p == "https://www.w3.org/ns/activitystreams#Service");
}

public class ASActorConverter : ASSerializer.ListSingleObjectConverter<ASActor>;