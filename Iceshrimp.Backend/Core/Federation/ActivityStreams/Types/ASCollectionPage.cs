using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollectionPage : ASObject
{
	public const string ObjectType = $"{Constants.ActivityStreamsNs}#CollectionPage";

	[JsonConstructor]
	public ASCollectionPage(bool withType = true) => Type = withType ? ObjectType : null;

	[SetsRequiredMembers]
	public ASCollectionPage(string id, bool withType = false) : this(withType) => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASCollectionItemsConverter))]
	public List<ASObject>? Items { get; set; }

	[J($"{Constants.ActivityStreamsNs}#totalItems")]
	[JC(typeof(VC))]
	public ulong? TotalItems { get; set; }

	[J($"{Constants.ActivityStreamsNs}#partOf")]
	[JC(typeof(ASCollectionConverter))]
	public ASCollection? PartOf { get; set; }

	[J($"{Constants.ActivityStreamsNs}#prev")]
	[JC(typeof(ASCollectionPageConverter))]
	public ASCollectionPage? Prev { get; set; }

	[J($"{Constants.ActivityStreamsNs}#next")]
	[JC(typeof(ASCollectionPageConverter))]
	public ASCollectionPage? Next { get; set; }
}

public sealed class ASCollectionPageConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return true;
	}

	public override object? ReadJson(
		JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer
	) => ASCollectionConverter.HandleCollectionObject<ASCollectionPage>(reader, objectType);

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}