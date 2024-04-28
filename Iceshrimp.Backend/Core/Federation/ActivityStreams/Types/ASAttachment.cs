using Iceshrimp.Backend.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class ASAttachment : ASObjectBase
{
	[J("@type")]
	[JC(typeof(StringListSingleConverter))]
	public string? Type { get; set; }
}

public class ASDocument : ASAttachment
{
	public ASDocument() => Type = $"{Constants.ActivityStreamsNs}#Document";

	[J($"{Constants.ActivityStreamsNs}#url")]
	[JC(typeof(ASLinkConverter))]
	public ASLink? Url { get; set; }

	[J($"{Constants.ActivityStreamsNs}#mediaType")]
	[JC(typeof(ValueObjectConverter))]
	public string? MediaType { get; set; }

	[J($"{Constants.ActivityStreamsNs}#sensitive")]
	[JC(typeof(ValueObjectConverter))]
	public bool? Sensitive { get; set; }

	[J($"{Constants.ActivityStreamsNs}#name")]
	[JC(typeof(ValueObjectConverter))]
	public string? Description { get; set; }
}

public class ASImage : ASDocument
{
	public ASImage() => Type = $"{Constants.ActivityStreamsNs}#Image";
}

public class ASField : ASAttachment
{
	[J($"{Constants.ActivityStreamsNs}#name")] [JC(typeof(ValueObjectConverter))]
	public string? Name;

	[J($"{Constants.SchemaNs}#value")] [JC(typeof(ValueObjectConverter))]
	public string? Value;

	public ASField() => Type = $"{Constants.SchemaNs}#PropertyValue";
}

public class ASImageConverter : ASSerializer.ListSingleObjectConverter<ASImage>;

public sealed class ASAttachmentConverter : JsonConverter
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
			var result = new List<ASAttachment>();
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

	private static ASAttachment? HandleObject(JToken obj)
	{
		var attachment = obj.ToObject<ASAttachment?>();

		return attachment?.Type switch
		{
			$"{Constants.ActivityStreamsNs}#Document" => obj.ToObject<ASDocument?>(),
			$"{Constants.ActivityStreamsNs}#Image"    => obj.ToObject<ASImage?>(),
			$"{Constants.SchemaNs}#PropertyValue"     => obj.ToObject<ASField?>(),
			_                                         => attachment
		};
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}