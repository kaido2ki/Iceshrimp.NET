using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASCollection : ASObject
{
	public const string ObjectType = $"{Constants.ActivityStreamsNs}#Collection";

	[JsonConstructor]
	public ASCollection(bool withType = true) => Type = withType ? ObjectType : null;

	[SetsRequiredMembers]
	public ASCollection(string id, bool withType = false) : this(withType) => Id = id;

	[J($"{Constants.ActivityStreamsNs}#items")]
	[JC(typeof(ASCollectionItemsConverter))]
	public List<ASObject>? Items { get; set; }

	[J($"{Constants.ActivityStreamsNs}#totalItems")]
	[JC(typeof(VC))]
	public ulong? TotalItems { get; set; }

	[J($"{Constants.ActivityStreamsNs}#current")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Current { get; set; }

	[J($"{Constants.ActivityStreamsNs}#first")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? First { get; set; }

	[J($"{Constants.ActivityStreamsNs}#last")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Last { get; set; }

	public new bool IsUnresolved => !TotalItems.HasValue;
}

public sealed class ASCollectionConverter : JsonConverter
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
				if (valueList is { Count: > 0 })
					return VC.HandleObject(valueList[0], objectType);
			}
			catch
			{
				// ignored
			}

			var list = obj.ToObject<List<ASCollection?>>();
			return list == null || list.Count == 0 ? null : list[0];
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj = JObject.Load(reader);
			try
			{
				var valueObj = obj.ToObject<LDValueObject<object?>>();
				if (valueObj != null)
					return VC.HandleObject(valueObj, objectType);
			}
			catch
			{
				// ignored
			}

			return obj.ToObject<ASCollection?>();
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}

internal class ASCollectionItemsConverter : JsonConverter
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
		if (reader.TokenType != JsonToken.StartArray) throw new Exception("this shouldn't happen");

		var obj = JArray.Load(reader);
		return obj.Count == 0
			? null
			: obj.Select(ASObject.Deserialize).OfType<ASObject>().ToList();
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}