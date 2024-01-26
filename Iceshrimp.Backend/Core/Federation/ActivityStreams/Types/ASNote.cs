using Iceshrimp.Backend.Core.Database.Tables;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASNote : ASObject {
	[J("https://misskey-hub.net/ns#_misskey_content")]
	[JC(typeof(VC))]
	public string? MkContent { get; set; }

	[J("https://www.w3.org/ns/activitystreams#content")]
	[JC(typeof(VC))]
	public string? Content { get; set; }

	[J("https://www.w3.org/ns/activitystreams#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J("https://www.w3.org/ns/activitystreams#sensitive")]
	[JC(typeof(VC))]
	public bool? Sensitive { get; set; }

	[J("https://www.w3.org/ns/activitystreams#published")]
	[JC(typeof(VC))]
	public DateTime? PublishedAt { get; set; }

	[J("https://www.w3.org/ns/activitystreams#source")]
	[JC(typeof(ASNoteSourceConverter))]
	public ASNoteSource? Source { get; set; }

	[J("https://www.w3.org/ns/activitystreams#to")]
	public List<LDIdObject>? To { get; set; } = [];

	[J("https://www.w3.org/ns/activitystreams#cc")]
	public List<LDIdObject>? Cc { get; set; } = [];

	[J("https://www.w3.org/ns/activitystreams#attributedTo")]
	public List<LDIdObject>? AttributedTo { get; set; } = [];

	public Note.NoteVisibility? Visibility => Note.NoteVisibility.Public;
}