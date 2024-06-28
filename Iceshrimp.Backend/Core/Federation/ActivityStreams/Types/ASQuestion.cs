using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASQuestion : ASNote
{
	private List<ASQuestionOption>? _anyOf;
	private List<ASQuestionOption>? _oneOf;
	public ASQuestion() => Type = Types.Question;

	[J($"{Constants.ActivityStreamsNs}#oneOf")]
	public List<ASQuestionOption>? OneOf
	{
		get => _oneOf;
		set => _oneOf = value?[..Math.Min(10, value.Count)];
	}

	[J($"{Constants.ActivityStreamsNs}#anyOf")]
	public List<ASQuestionOption>? AnyOf
	{
		get => _anyOf;
		set => _anyOf = value?[..Math.Min(10, value.Count)];
	}

	[J($"{Constants.ActivityStreamsNs}#endTime")]
	[JC(typeof(VC))]
	public DateTime? EndTime { get; set; }

	[J($"{Constants.ActivityStreamsNs}#closed")]
	[JC(typeof(VC))]
	public DateTime? Closed { get; set; }

	[J($"{Constants.MastodonNs}#votersCount")]
	[JC(typeof(VC))]
	public int? VotersCount { get; set; }

	public class ASQuestionOption : ASObjectBase
	{
		[J("@type")]
		[JC(typeof(StringListSingleConverter))]
		public string Type => ASNote.Types.Note;

		[J($"{Constants.ActivityStreamsNs}#name")]
		[JC(typeof(VC))]
		public string? Name { get; set; }

		[J($"{Constants.ActivityStreamsNs}#replies")]
		[JC(typeof(ASCollectionBaseConverter))]
		public ASCollectionBase? Replies { get; set; }
	}

	public new static class Types
	{
		private const string Ns = Constants.ActivityStreamsNs;

		public const string Question = $"{Ns}#Question";
	}
}