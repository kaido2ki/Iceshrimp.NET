using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using JI = Newtonsoft.Json.JsonIgnoreAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection : ASCollection
{
	[JsonConstructor]
	public ASOrderedCollection(bool withType = true) => Type = withType ? ObjectType : null;

	[SetsRequiredMembers]
	public ASOrderedCollection(string id, bool withType = false) : this(withType) => Id = id;

	[J($"{Constants.ActivityStreamsNs}#orderedItems")]
	[JC(typeof(ASCollectionItemsConverter))]
	[JI]
	public List<ASObject>? OrderedItems
	{
		get => Items;
		set => Items = value;
	}

	public new const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollection";
}

public sealed class ASOrderedCollectionConverter : JsonConverter
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
			if (valueList?.Any(p => p.Value != null) ?? false)
				return ValueObjectConverter.HandleObject(valueList[0], objectType);
			var list = obj.ToObject<List<ASOrderedCollection?>>();
			return list == null || list.Count == 0 ? null : list[0];
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj      = JObject.Load(reader);
			var valueObj = obj.ToObject<LDValueObject<object?>>();
			return valueObj is { Value: not null }
				? ValueObjectConverter.HandleObject(valueObj, objectType)
				: obj.ToObject<ASOrderedCollection?>();
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}