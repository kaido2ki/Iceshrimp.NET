using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActor : ASObject {
	private const int DisplayNameLength = 128;
	private const int UsernameLength    = 128;
	private const int SummaryLength     = 2048;

	private static readonly List<string> ActorTypes = [
		Types.Person, Types.Service, Types.Group, Types.Organization, Types.Application
	];

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
	public ASCollection<ASObject>? Outbox { get; set; }

	[J("http://www.w3.org/ns/ldp#inbox")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Inbox { get; set; }

	[J("https://www.w3.org/ns/activitystreams#followers")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection<ASObject>? Followers { get; set; }

	[J("https://www.w3.org/ns/activitystreams#following")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection<ASObject>? Following { get; set; }

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
	public List<ASLink>? AlsoKnownAs { get; set; } = [];

	[J("http://joinmastodon.org/ns#featured")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Featured { get; set; }

	[J("http://joinmastodon.org/ns#featuredTags")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? FeaturedTags { get; set; }

	[J("https://www.w3.org/ns/activitystreams#icon")]
	[JC(typeof(ASImageConverter))]
	public ASImage? Avatar { get; set; }

	[J("https://www.w3.org/ns/activitystreams#image")]
	[JC(typeof(ASImageConverter))]
	public ASImage? Banner { get; set; }

	[J("https://w3id.org/security#publicKey")]
	[JC(typeof(ASPublicKeyConverter))]
	public ASPublicKey? PublicKey { get; set; }

	public bool IsBot => Type == "https://www.w3.org/ns/activitystreams#Service";

	public void Normalize(string uri, string acct) {
		if (Type == null || !ActorTypes.Contains(Type)) throw new Exception("Actor is of invalid type");

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

	public new static class Types {
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Application  = $"{Ns}#Application";
		public const string Group        = $"{Ns}#Group";
		public const string Organization = $"{Ns}#Organization";
		public const string Person       = $"{Ns}#Person";
		public const string Service      = $"{Ns}#Service";
	}

	public static ASActor FromObject(ASObject obj) {
		return new ASActor {
			Id = obj.Id
		};
	}

	public ASActor Compact() {
		return new ASActor {
			Id = Id
		};
	}
}

public class ASActorConverter : ASSerializer.ListSingleObjectConverter<ASActor>;