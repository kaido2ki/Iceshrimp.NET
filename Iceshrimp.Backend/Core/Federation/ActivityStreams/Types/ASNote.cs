using Iceshrimp.Backend.Core.Configuration;
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
	public List<ASObjectBase> To { get; set; } = [];

	[J("https://www.w3.org/ns/activitystreams#cc")]
	public List<ASObjectBase> Cc { get; set; } = [];

	[J("https://www.w3.org/ns/activitystreams#attributedTo")]
	public List<ASObjectBase> AttributedTo { get; set; } = [];

	[J("https://www.w3.org/ns/activitystreams#inReplyTo")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? InReplyTo { get; set; }

	[J("https://www.w3.org/ns/activitystreams#tag")]
	[JC(typeof(ASTagConverter))]
	public List<ASTag>? Tags { get; set; }

	public Note.NoteVisibility GetVisibility(ASActor actor) {
		if (To.Any(p => p.Id == "https://www.w3.org/ns/activitystreams#Public"))
			return Note.NoteVisibility.Public;
		if (Cc.Any(p => p.Id == "https://www.w3.org/ns/activitystreams#Public"))
			return Note.NoteVisibility.Home;
		if (To.Any(p => p.Id is not null && p.Id == (actor.Followers?.Id ?? actor.Id + "/followers")))
			return Note.NoteVisibility.Followers;

		return Note.NoteVisibility.Specified;
	}

	public List<string> GetRecipients(ASActor actor) {
		return To.Concat(Cc)
		         .Select(p => p.Id)
		         .Distinct()
		         .Where(p => p != $"{Constants.ActivityStreamsNs}#Public" &&
		                     p != (actor.Followers?.Id ?? actor.Id + "/followers"))
		         .Where(p => p != null)
		         .Select(p => p!)
		         .ToList();
	}

	public new static class Types {
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Note = $"{Ns}#Note";
	}
}