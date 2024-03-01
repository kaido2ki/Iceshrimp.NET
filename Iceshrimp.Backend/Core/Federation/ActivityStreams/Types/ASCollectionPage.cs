using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollectionPage : ASObject
{
	public ASCollectionPage() => Type = ObjectType;

	[SetsRequiredMembers]
	public ASCollectionPage(string id) : this() => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASCollectionItemsConverter))]
	public List<ASObject>? Items { get; set; }

	[J($"{Constants.ActivityStreamsNs}#totalItems")]
	[JC(typeof(VC))]
	public ulong? TotalItems { get; set; }

	[J($"{Constants.ActivityStreamsNs}#partOf")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? PartOf { get; set; }

	[J($"{Constants.ActivityStreamsNs}#prev")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Prev { get; set; }

	[J($"{Constants.ActivityStreamsNs}#next")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Next { get; set; }

	public const string ObjectType = $"{Constants.ActivityStreamsNs}#CollectionPage";
}

public sealed class ASCollectionPageConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
	{
		return true;
	}

	public override object? ReadJson(
		JsonReader reader, Type objectType, object? existingValue,
		JsonSerializer serializer
	)
	{
		if (reader.TokenType == JsonToken.StartArray)
		{
			var obj       = JArray.Load(reader);
			var valueList = obj.ToObject<List<LDValueObject<object?>>>();
			if (valueList is { Count: > 0 })
				return VC.HandleObject(valueList[0], objectType);
			var list = obj.ToObject<List<ASCollectionPage?>>();
			return list == null || list.Count == 0 ? null : list[0];
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj      = JObject.Load(reader);
			var valueObj = obj.ToObject<LDValueObject<object?>>();
			return valueObj != null
				? VC.HandleObject(valueObj, objectType)
				: obj.ToObject<ASCollectionPage?>();
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}