using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASTag : ASObjectBase
{
	[J("@type")]
	[JC(typeof(StringListSingleConverter))]
	public string? Type { get; set; }
}

public class ASTagLink : ASTag
{
	[J($"{Constants.ActivityStreamsNs}#href")]
	[JC(typeof(ASObjectBaseConverter))]
	public ASObjectBase? Href { get; set; }

	[J($"{Constants.ActivityStreamsNs}#name")]
	[JC(typeof(VC))]
	public string? Name { get; set; }
}

public class ASMention : ASTagLink
{
	public ASMention() => Type = $"{Constants.ActivityStreamsNs}#Mention";
}

public class ASHashtag : ASTagLink
{
	public ASHashtag() => Type = $"{Constants.ActivityStreamsNs}#Hashtag";
}

public class ASEmoji : ASTag
{
	public ASEmoji() => Type = $"{Constants.MastodonNs}#Emoji";

	[J($"{Constants.ActivityStreamsNs}#updated")]
	[JC(typeof(VC))]
	public DateTime? Updated { get; set; }

	[J($"{Constants.ActivityStreamsNs}#icon")]
	[JC(typeof(ASImageConverter))]
	public ASImage? Image { get; set; }

	[J($"{Constants.ActivityStreamsNs}#name")]
	[JC(typeof(VC))]
	public string? Name { get; set; }
}

public sealed class ASTagConverter : JsonConverter
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
		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj = JObject.Load(reader);
			return HandleObject(obj);
		}

		if (reader.TokenType == JsonToken.StartArray)
		{
			var array  = JArray.Load(reader);
			var result = new List<ASTag>();
			foreach (var token in array)
			{
				if (token is not JObject obj) return null;
				var item = HandleObject(obj);
				if (item == null) return null;
				result.Add(item);
			}

			return result;
		}

		return null;
	}

	private ASTag? HandleObject(JToken obj)
	{
		var tag = obj.ToObject<ASTag?>();

		return tag?.Type switch
		{
			$"{Constants.ActivityStreamsNs}#Mention" => obj.ToObject<ASMention?>(),
			$"{Constants.ActivityStreamsNs}#Hashtag" => obj.ToObject<ASHashtag?>(),
			$"{Constants.MastodonNs}#Emoji"          => obj.ToObject<ASEmoji?>(),
			_                                        => null
		};
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}