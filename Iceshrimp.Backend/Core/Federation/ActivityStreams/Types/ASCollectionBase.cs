using Iceshrimp.Backend.Core.Configuration;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollectionBase : ASObjectBase
{
	public const string ObjectType = $"{Constants.ActivityStreamsNs}#Collection";

	[J("@type")]
	[JC(typeof(StringListSingleConverter))]
	public string Type => ObjectType;

	[J($"{Constants.ActivityStreamsNs}#totalItems")]
	[JC(typeof(VC))]
	public ulong? TotalItems { get; set; }
}

public sealed class ASCollectionBaseConverter : ASSerializer.ListSingleObjectConverter<ASCollectionBase>;