using System.Globalization;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

public class LDLocalizedString
{
	/// <summary>
	/// 	key: BCP 47 language code, with empty string meaning "unknown"
	/// 	value: content, in that language
	/// </summary>
	public Dictionary<string, string?> Values { get; set; }

	public LDLocalizedString()
	{
		Values = [];
	}

	public LDLocalizedString(string? language, string? value)
	{
		Values = [];

		// this is required to create a non-Map field for non-JsonLD remotes.
		Values.Add("", value);

		if (language != null)
		{
			language = NormalizeLanguageCode(language);

			if (language != null)
				Values.Add(language, value);
		}
	}

	/// <summary>
	/// 	If the remote sends different content to multiple languages, try and guess which one they prefer "by default".
	/// 	
	/// 	This idea was taken from Sharkey's implementation.
	/// 	https://activitypub.software/TransFem-org/Sharkey/-/merge_requests/401#note_1174
	/// </summary>
	/// <param name="language">The guessed language</param>
	/// <returns>The value, in the guessed language</returns>
	public string? GuessPreferredValue(out string? language)
	{
		if (Values.Count > 1)
		{
			var unknownValue = GetUnknownValue();
			if (unknownValue != null)
			{
				var preferred = Values.FirstOrDefault(i => !IsUnknownLanguage(i.Key) && i.Value == unknownValue);
				if (preferred.Value != null)
				{
					language = NormalizeLanguageCode(preferred.Key);
					return preferred.Value;
				}
			}
		}
		else if (Values.Count == 0)
		{
			language = null;
			return null;
		}

		var first = Values.FirstOrDefault(i => !IsUnknownLanguage(i.Key));
		if (first.Value == null)
		{
			first = Values.FirstOrDefault();
			if (first.Value == null)
			{
				language = null;
				return null;
			}
		}

		language = IsUnknownLanguage(first.Key) ? null : NormalizeLanguageCode(first.Key);
		return first.Value;
	}

	public static string? NormalizeLanguageCode(string lang)
	{
		try
		{
			return CultureInfo.CreateSpecificCulture(lang).ToString();
		}
		catch (CultureNotFoundException)
		{
			// invalid language code
			return null;
		}
	}

	// Akkoma forces all non-localized text to be in the "und" language by adding { "@language":"und" } to it's context
	public static bool IsUnknownLanguage(string? lang) => lang == null || lang == "" || lang == "und";
	public string? GetUnknownValue()
	{
		string? value;

		if (Values.TryGetValue("", out value))
			return value;

		if (Values.TryGetValue("und", out value))
			return value;

		return null;
	}
}

public class LDValueObject<T>
{
	[J("@type")]      public          string? Type  { get; set; }
	[J("@value")]     public required T       Value { get; set; }
	[J("@language")]  public          string? Language { get; set; }
}

public class ValueObjectConverter : JsonConverter
{
	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		return true;
	}

	public override object? ReadJson(
		JsonReader reader, Type objectType, object? existingValue,
		JsonSerializer serializer
	)
	{
		// TODO: this feels wrong
		if (reader.TokenType == JsonToken.StartArray && objectType == typeof(LDLocalizedString))
		{
			var obj  = JArray.Load(reader);
			var list = obj.ToObject<List<LDValueObject<string?>>>();
			if (list == null || list.Count == 0)
				return null;

			var localized = new LDLocalizedString();

			foreach (var item in list)
			{
				localized.Values.Add(item.Language ?? "", item.Value);
			}

			return localized;
		}

		if (reader.TokenType == JsonToken.StartArray)
		{
			var obj  = JArray.Load(reader);
			var list = obj.ToObject<List<LDValueObject<object?>>>();
			if (list == null || list.Count == 0)
				return null;
			return HandleObject(list[0], objectType);
		}

		if (reader.TokenType == JsonToken.StartObject)
		{
			var obj      = JObject.Load(reader);
			var finalObj = obj.ToObject<LDValueObject<object?>>();
			return HandleObject(finalObj, objectType);
		}

		return null;
	}

	internal static object? HandleObject(LDValueObject<object?>? obj, Type objectType)
	{
		if (obj?.Value is string s && (objectType == typeof(DateTime?) || objectType == typeof(DateTime)))
		{
			var succeeded = DateTime.TryParse(s, out var result);
			return succeeded ? result : null;
		}

		if (objectType == typeof(uint?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToUInt32(val) : null;
		}

		if (objectType == typeof(ulong?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToUInt64(val) : null;
		}
		
		if (objectType == typeof(int?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToInt32(val) : null;
		}

		if (objectType == typeof(long?))
		{
			var val = obj?.Value;
			return val != null ? Convert.ToInt64(val) : null;
		}

		if (obj?.Value is string id)
		{
			if (objectType == typeof(ASOrderedCollection))
				return new ASOrderedCollection(id);
			if (objectType == typeof(ASCollection))
				return new ASCollection(id);
		}

		return obj?.Value;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{

		// TODO: this also feels wrong
		if (value is LDLocalizedString lstr) {
			writer.WriteStartArray();

			foreach (var item in lstr.Values)
			{
				writer.WriteStartObject();

				if (!LDLocalizedString.IsUnknownLanguage(item.Key))
				{
					writer.WritePropertyName("@language");
					writer.WriteValue(item.Key);
				}

				writer.WritePropertyName("@value");
				writer.WriteValue(item.Value);

				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			return;
		}

		writer.WriteStartObject();

		switch (value)
		{
			case DateTime dt:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#dateTime");
				writer.WritePropertyName("@value");
				writer.WriteValue(dt.ToStringIso8601Like());
				break;
			case uint ui:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#nonNegativeInteger");
				writer.WritePropertyName("@value");
				writer.WriteValue(ui);
				break;
			case ulong ul:
				writer.WritePropertyName("@type");
				writer.WriteValue($"{Constants.XsdNs}#nonNegativeInteger");
				writer.WritePropertyName("@value");
				writer.WriteValue(ul);
				break;
			case ASOrderedCollection oc:
				writer.WritePropertyName("@type");
				writer.WriteValue(ASOrderedCollection.ObjectType);
				writer.WritePropertyName("@value");
				writer.WriteRawValue(JsonConvert.SerializeObject(oc));
				break;
			case ASCollection c:
				writer.WritePropertyName("@type");
				writer.WriteValue(ASCollection.ObjectType);
				writer.WritePropertyName("@value");
				writer.WriteRawValue(JsonConvert.SerializeObject(c));
				break;
			default:
				writer.WritePropertyName("@value");
				writer.WriteValue(value);
				break;
		}

		writer.WriteEndObject();
	}
}