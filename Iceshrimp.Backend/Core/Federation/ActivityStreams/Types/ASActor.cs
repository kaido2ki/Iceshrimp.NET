using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Helpers;
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

	[J("https://www.w3.org/ns/activitystreams#movedTo")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? MovedTo { get; set; }

	[J("https://www.w3.org/ns/activitystreams#alsoKnownAs")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? AlsoKnownAs { get; set; }

	[J("http://joinmastodon.org/ns#featured")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Featured { get; set; }

	[J("http://joinmastodon.org/ns#featuredTags")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? FeaturedTags { get; set; }

	public bool IsBot => Type?.Any(p => p == "https://www.w3.org/ns/activitystreams#Service") ?? false;

	private const int DisplayNameLength = 128;
	private const int UsernameLength    = 128;
	private const int SummaryLength     = 2048;

	private static readonly List<string> ActorTypes = [
		"https://www.w3.org/ns/activitystreams#Person",
		"https://www.w3.org/ns/activitystreams#Service",
		"https://www.w3.org/ns/activitystreams#Group",
		"https://www.w3.org/ns/activitystreams#Organization",
		"https://www.w3.org/ns/activitystreams#Application"
	];

	public void Normalize(string uri, string acct) {
		if (!Type?.Any(t => ActorTypes.Contains(t)) ?? false) throw new Exception("Actor is of invalid type");

		// in case this is ever removed - check for hostname match instead
		if (Id != uri) throw new Exception("Actor URI mismatch");

		if (Inbox?.Link == null) throw new Exception("Actor inbox is invalid");
		if (Username == null || Username.Length > UsernameLength ||
		    !Regex.IsMatch(Username, @"^\w([\w-.]*\w)?$"))
			throw new Exception("Actor username is invalid");

		//TODO: validate publicKey id host

		DisplayName = DisplayName switch {
			{ Length: > 0 } => DisplayName.Truncate(DisplayNameLength),
			_               => null
		};

		Summary = Summary switch {
			{ Length: > 0 } => Summary.Truncate(SummaryLength),
			_               => null
		};
	}
}

public class ASActorConverter : ASSerializer.ListSingleObjectConverter<ASActor>;