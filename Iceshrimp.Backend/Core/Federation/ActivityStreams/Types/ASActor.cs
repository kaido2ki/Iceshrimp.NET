using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASActor : ASObject
{
	private const int DisplayNameLength = 128;
	private const int UsernameLength    = 128;
	private const int SummaryLength     = 2048;

	private static readonly List<string> ActorTypes =
	[
		Types.Person, Types.Service, Types.Group, Types.Organization, Types.Application
	];

	[J("https://misskey-hub.net/ns#_misskey_summary")]
	[JC(typeof(VC))]
	public string? MkSummary { get; set; }

	[J($"{Constants.ActivityStreamsNs}#summary")]
	[JC(typeof(VC))]
	public string? Summary { get; set; }

	[J($"{Constants.ActivityStreamsNs}#name")]
	[JC(typeof(VC))]
	public string? DisplayName { get; set; }

	[J($"{Constants.ActivityStreamsNs}#preferredUsername")]
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

	[J($"{Constants.ActivityStreamsNs}#manuallyApprovesFollowers")]
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

	[J($"{Constants.ActivityStreamsNs}#endpoints")]
	[JC(typeof(ASEndpointsConverter))]
	public ASEndpoints? Endpoints { get; set; }

	[J($"{Constants.ActivityStreamsNs}#outbox")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection? Outbox { get; set; }

	[J("http://www.w3.org/ns/ldp#inbox")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Inbox { get; set; }

	[J($"{Constants.ActivityStreamsNs}#followers")]
	[JC(typeof(ASOrderedCollectionConverter))]
	public ASOrderedCollection? Followers { get; set; }

	[J($"{Constants.ActivityStreamsNs}#following")]
	[JC(typeof(ASOrderedCollectionConverter))]
	public ASOrderedCollection? Following { get; set; }

	[J($"{Constants.ActivityStreamsNs}#sharedInbox")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? SharedInbox { get; set; }

	[J($"{Constants.ActivityStreamsNs}#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J($"{Constants.ActivityStreamsNs}#movedTo")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? MovedTo { get; set; }

	[J($"{Constants.ActivityStreamsNs}#alsoKnownAs")]
	public List<ASLink>? AlsoKnownAs { get; set; }

	[J("http://joinmastodon.org/ns#featured")]
	[JC(typeof(ASOrderedCollectionConverter))]
	public ASOrderedCollection? Featured { get; set; }

	[J("http://joinmastodon.org/ns#featuredTags")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? FeaturedTags { get; set; }

	[J($"{Constants.ActivityStreamsNs}#icon")]
	[JC(typeof(ASImageConverter))]
	public ASImage? Avatar { get; set; }

	[J($"{Constants.ActivityStreamsNs}#image")]
	[JC(typeof(ASImageConverter))]
	public ASImage? Banner { get; set; }

	[J($"{Constants.W3IdSecurityNs}#publicKey")]
	[JC(typeof(ASPublicKeyConverter))]
	public ASPublicKey? PublicKey { get; set; }

	[J($"{Constants.ActivityStreamsNs}#tag")]
	[JC(typeof(ASTagConverter))]
	public List<ASTag>? Tags { get; set; }

	[J($"{Constants.ActivityStreamsNs}#attachment")]
	[JC(typeof(ASAttachmentConverter))]
	public List<ASAttachment>? Attachments { get; set; }

	[JI] public bool IsBot => Type == $"{Constants.ActivityStreamsNs}#Service";

	public void Normalize(string uri)
	{
		if (Type == null || !ActorTypes.Contains(Type)) throw new Exception("Actor is of invalid type");

		// in case this is ever removed - check for hostname match instead
		if (Id != uri) throw new Exception("Actor URI mismatch");

		if (Inbox?.Link == null) throw new Exception("Actor inbox is invalid");
		if (Username == null ||
		    Username.Length > UsernameLength ||
		    !Regex.IsMatch(Username, @"^\w([\w-.]*\w)?$"))
			throw new Exception("Actor username is invalid");

		var publicKeyId = PublicKey?.Id ?? throw new Exception("Invalid actor: missing PublicKey?.Id");
		if (new Uri(publicKeyId).Host != new Uri(uri).Host)
			throw new Exception("Invalid actor: public key id / actor id host mismatch");

		DisplayName = DisplayName switch
		{
			{ Length: > 0 } => DisplayName.Truncate(DisplayNameLength),
			_               => null
		};

		Summary = Summary switch
		{
			{ Length: > 0 } => Summary.Truncate(SummaryLength),
			_               => null
		};
	}

	public static ASActor FromObject(ASObject obj)
	{
		return new ASActor { Id = obj.Id };
	}

	public ASActor Compact()
	{
		return new ASActor { Id = Id };
	}

	public new static class Types
	{
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Application  = $"{Ns}#Application";
		public const string Group        = $"{Ns}#Group";
		public const string Organization = $"{Ns}#Organization";
		public const string Person       = $"{Ns}#Person";
		public const string Service      = $"{Ns}#Service";
	}
}

public class ASActorConverter : ASSerializer.ListSingleObjectConverter<ASActor>;