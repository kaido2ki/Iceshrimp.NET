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

	public override object? ReadJson(
		JsonReader reader, Type objectType, object? existingValue,
		JsonSerializer serializer
	)
	{
		if (reader.TokenType != JsonToken.StartArray) throw new Exception("this shouldn't happen");

		var obj = JArray.Load(reader);
		return obj.Count == 0
			? null
			: obj.SelectToken("$.[*].@list")?.Children().Select(ASObject.Deserialize).OfType<ASObject>().ToList();
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
		JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer
	) => ASCollectionConverter.HandleCollectionObject<ASOrderedCollection>(reader, objectType);

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}