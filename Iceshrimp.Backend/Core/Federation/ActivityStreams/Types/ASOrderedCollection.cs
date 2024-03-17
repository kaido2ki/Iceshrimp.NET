using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASOrderedCollection : ASCollection
{
	public new const string ObjectType = $"{Constants.ActivityStreamsNs}#OrderedCollection";

	[JsonConstructor]
	public ASOrderedCollection(bool withType = true) => Type = withType ? ObjectType : null;

	[SetsRequiredMembers]
	public ASOrderedCollection(string id, bool withType = false) : this(withType) => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASOrderedCollectionItemsConverter))]
	public new List<ASObject>? Items
	{
		get => base.Items;
		set => base.Items = value;
	}
}

internal sealed class ASOrderedCollectionItemsConverter : ASCollectionItemsConverter
{
	public override bool CanWrite => true;

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		writer.WriteStartObject();
		writer.WritePropertyName("@list");
		serializer.Serialize(writer, value);
		writer.WriteEndObject();
	}
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
			var obj = JArray.Load(reader);
			try
			{
				var valueList = obj.ToObject<List<LDValueObject<object?>>>();
				if (valueList?.Any(p => p.Value != null) ?? false)
					return ValueObjectConverter.HandleObject(valueList[0], objectType);
			}
			catch
			{
				//ignored
			}

			var list = obj.ToObject<List<ASOrderedCollection?>>();
			return list == null || list.Count == 0 ? null : list[0];
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj = JObject.Load(reader);
			try { }
			catch
			{
				var valueObj = obj.ToObject<LDValueObject<object?>>();
				if (valueObj is { Value: not null })
					return ValueObjectConverter.HandleObject(valueObj, objectType);
			}

			return obj.ToObject<ASOrderedCollection?>();
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}