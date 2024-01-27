using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF.JsonLd;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams;

public static class LdHelpers {
	private static readonly Dictionary<string, RemoteDocument> ContextCache = new() {
		{
			"https://www.w3.org/ns/activitystreams", new RemoteDocument {
				ContextUrl  = new Uri("https://www.w3.org/ns/activitystreams"),
				DocumentUrl = new Uri("https://www.w3.org/ns/activitystreams"),
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "as.json")))
			}
		}, {
			"https://w3id.org/security/v1", new RemoteDocument {
				ContextUrl  = new Uri("https://w3id.org/security/v1"),
				DocumentUrl = new Uri("https://w3c-ccg.github.io/security-vocab/contexts/security-v1.jsonld"),
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "security.json")))
			}
		}, {
			"http://joinmastodon.org/ns", new RemoteDocument {
				ContextUrl  = new Uri("http://joinmastodon.org/ns"),
				DocumentUrl = new Uri("http://joinmastodon.org/ns"),
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "toot.json")))
			}
		}
	};

	private static readonly JToken DefaultContext =
		JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams", "Contexts",
		                                           "default.json")));

	// Nonstandard extensions to the AS context need to be loaded in to fix federation with certain AP implementations
	private static readonly JToken ASExtensions =
		JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams", "Contexts",
		                                           "as-extensions.json")));

	private static readonly JsonLdProcessorOptions Options = new() {
		DocumentLoader = CustomLoader, ExpandContext = ASExtensions
	};

	private static readonly JsonSerializerSettings JsonSerializerSettings = new() {
		NullValueHandling    = NullValueHandling.Ignore,
		DateTimeZoneHandling = DateTimeZoneHandling.Local
	};

	private static readonly JsonSerializer JsonSerializer = new() {
		NullValueHandling    = NullValueHandling.Ignore,
		DateTimeZoneHandling = DateTimeZoneHandling.Local
	};

	private static RemoteDocument CustomLoader(Uri uri, JsonLdLoaderOptions jsonLdLoaderOptions) {
		//TODO: cache in redis
		ContextCache.TryGetValue(uri.ToString(), out var result);
		if (result != null) return result;
		result = DefaultDocumentLoader.LoadJson(uri, jsonLdLoaderOptions);
		ContextCache.Add(uri.ToString(), result);

		return result;
	}

	public static async Task<string> SignAndCompact(this ASActivity activity, UserKeypair keypair) {
		var expanded = Expand(activity) ?? throw new Exception("Failed to expand activity");
		var signed = await LdSignature.Sign(expanded, keypair.PrivateKey,
		                                    activity.Actor?.PublicKey?.Id ?? $"{activity.Actor!.Id}#main-key") ??
		             throw new Exception("Failed to sign activity");
		var compacted = Compact(signed) ?? throw new Exception("Failed to compact signed activity");
		var payload   = JsonConvert.SerializeObject(compacted, JsonSerializerSettings);

		return payload;
	}

	public static JObject? Compact(object obj) {
		return Compact(JToken.FromObject(obj, JsonSerializer));
	}

	public static JArray? Expand(object obj) {
		return Expand(JToken.FromObject(obj, JsonSerializer));
	}

	public static JObject? Compact(JToken? json) {
		return JsonLdProcessor.Compact(json, DefaultContext, Options);
	}

	public static JArray? Expand(JToken? json) {
		return JsonLdProcessor.Expand(json, Options);
	}

	public static string Canonicalize(JArray json) {
		return JsonLdProcessor.Canonicalize(json);
	}

	public static string Canonicalize(JObject json) {
		return JsonLdProcessor.Canonicalize([json]);
	}
}