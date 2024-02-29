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
	public ASCollection() => Type = ObjectType;

	[SetsRequiredMembers]
	public ASCollection(string id) : this() => Id = id;

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

	public const string ObjectType = $"{Constants.ActivityStreamsNs}#Collection";
}

public sealed class ASCollectionConverter : ASSerializer.ListSingleObjectConverter<ASCollection>;

internal sealed class ASCollectionItemsConverter : JsonConverter
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
			: obj.SelectToken("$.[*].@list")?.Children().Select(ASObject.Deserialize).OfType<ASObject>().ToList();
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}