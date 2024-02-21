using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASNote : ASObject
{
	public ASNote() => Type = Types.Note;

	[J("https://misskey-hub.net/ns#_misskey_content")]
	[JC(typeof(VC))]
	public string? MkContent { get; set; }

	[J("https://misskey-hub.net/ns#_misskey_quote")]
	[JC(typeof(VC))]
	public string? MkQuote { get; set; }

	[J($"{Constants.ActivityStreamsNs}#quoteUrl")]
	[JC(typeof(VC))]
	public string? QuoteUrl { get; set; }

	[J("http://fedibird.com/ns#quoteUri")]
	[JC(typeof(VC))]
	public string? QuoteUri { get; set; }

	[J($"{Constants.ActivityStreamsNs}#content")]
	[JC(typeof(VC))]
	public string? Content { get; set; }

	[J($"{Constants.ActivityStreamsNs}#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J($"{Constants.ActivityStreamsNs}#sensitive")]
	[JC(typeof(VC))]
	public bool? Sensitive { get; set; }

	[J($"{Constants.ActivityStreamsNs}#summary")]
	[JC(typeof(VC))]
	public string? Summary { get; set; }

	[J($"{Constants.ActivityStreamsNs}#published")]
	[JC(typeof(VC))]
	public DateTime? PublishedAt { get; set; }

	[J($"{Constants.ActivityStreamsNs}#source")]
	[JC(typeof(ASNoteSourceConverter))]
	public ASNoteSource? Source { get; set; }

	[J($"{Constants.ActivityStreamsNs}#to")]
	public List<ASObjectBase> To { get; set; } = [];

	[J($"{Constants.ActivityStreamsNs}#cc")]
	public List<ASObjectBase> Cc { get; set; } = [];

	[J($"{Constants.ActivityStreamsNs}#attributedTo")]
	public List<ASObjectBase> AttributedTo { get; set; } = [];

	[J($"{Constants.ActivityStreamsNs}#inReplyTo")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? InReplyTo { get; set; }

	[J($"{Constants.ActivityStreamsNs}#tag")]
	[JC(typeof(ASTagConverter))]
	public List<ASTag>? Tags { get; set; }

	[J($"{Constants.ActivityStreamsNs}#attachment")]
	[JC(typeof(ASAttachmentConverter))]
	public List<ASAttachment>? Attachments { get; set; }

	public bool VerifiedFetch = false;

	public Note.NoteVisibility GetVisibility(User actor)
	{
		if (actor.Host == null) throw new Exception("Can't get recipients for local actor");

		if (To.Any(p => p.Id == $"{Constants.ActivityStreamsNs}#Public"))
			return Note.NoteVisibility.Public;
		if (Cc.Any(p => p.Id == $"{Constants.ActivityStreamsNs}#Public"))
			return Note.NoteVisibility.Home;
		if (To.Any(p => p.Id is not null && p.Id == (actor.FollowersUri ?? actor.Uri + "/followers")))
			return Note.NoteVisibility.Followers;

		return Note.NoteVisibility.Specified;
	}

	public List<string> GetRecipients(User actor)
	{
		if (actor.Host == null) throw new Exception("Can't get recipients for local actor");
		return To.Concat(Cc)
		         .Select(p => p.Id)
		         .Distinct()
		         .Where(p => p != $"{Constants.ActivityStreamsNs}#Public" &&
		                     p != (actor.FollowersUri ?? actor.Uri + "/followers"))
		         .Where(p => p != null)
		         .Select(p => p!)
		         .ToList();
	}

	public new static class Types
	{
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Note = $"{Ns}#Note";
	}
}