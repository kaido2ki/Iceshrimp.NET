using System.Collections.Concurrent;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Federation.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF.JsonLd;
using VDS.RDF.JsonLd.Syntax;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams;

public static class LdHelpers
{
	private static readonly Dictionary<string, RemoteDocument> PreloadedContexts = new()
	{
		{
			"https://www.w3.org/ns/activitystreams", new RemoteDocument
			{
				DocumentUrl = new Uri("https://www.w3.org/ns/activitystreams"),
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "as.json")))
			}
		},
		{
			"https://w3id.org/security/v1", new RemoteDocument
			{
				DocumentUrl = new Uri("https://w3c-ccg.github.io/security-vocab/contexts/security-v1.jsonld"),
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "security.json")))
			}
		},
		{
			"http://joinmastodon.org/ns", new RemoteDocument
			{
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "toot.json")))
			}
		},
		{
			"http://schema.org/", new RemoteDocument
			{
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "schema.json")))
			}
		},
		{
			"litepub-0.1", new RemoteDocument
			{
				Document = JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams",
				                                                      "Contexts", "litepub.json")))
			}
		}
	};

	private static readonly ConcurrentDictionary<string, RemoteDocument> ContextCache = new();

	private static readonly JToken DefaultContext =
		JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams", "Contexts",
		                                           "default.json")));

	// Nonstandard extensions to the AS context need to be loaded in to fix federation with certain AP implementations
	private static readonly JToken ASExtensions =
		JToken.Parse(File.ReadAllText(Path.Combine("Core", "Federation", "ActivityStreams", "Contexts",
		                                           "as-extensions.json")));

	private static readonly JsonLdProcessorOptions Options = new()
	{
		DocumentLoader = CustomLoader,
		ExpandContext  = ASExtensions,
		ProcessingMode = JsonLdProcessingMode.JsonLd11,
		KeepIRIs       = [$"{Constants.ActivityStreamsNs}#Public"],
		ForceArray     = ASForceArray.Select(p => $"{Constants.ActivityStreamsNs}#{p}").ToList(),

		// separated for readability
		RemoveUnusedInlineContextProperties = true
	};

	public static readonly JsonSerializerSettings JsonSerializerSettings = new()
	{
		NullValueHandling = NullValueHandling.Ignore, DateTimeZoneHandling = DateTimeZoneHandling.Local
	};

	private static readonly JsonSerializer JsonSerializer = new()
	{
		NullValueHandling = NullValueHandling.Ignore, DateTimeZoneHandling = DateTimeZoneHandling.Local
	};

	private static IEnumerable<string> ASForceArray => ["tag", "to", "cc", "bcc", "bto"];

	private static RemoteDocument CustomLoader(Uri uri, JsonLdLoaderOptions jsonLdLoaderOptions)
	{
		var key = uri.AbsolutePath == "/schemas/litepub-0.1.jsonld" ? "litepub-0.1" : uri.ToString();
		if (!PreloadedContexts.TryGetValue(key, out var result))
			ContextCache.TryGetValue(key, out result);

		if (result != null)
		{
			result.ContextUrl = uri;
			return result;
		}

		//TODO: cache in postgres with ttl 24h
		result = DefaultDocumentLoader.LoadJson(uri, jsonLdLoaderOptions);
		ContextCache.TryAdd(uri.ToString(), result);

		// Cleanup to make sure this doesn't take up more and more memory
		while (ContextCache.Count > 20)
		{
			var hit = ContextCache.Keys.FirstOrDefault();
			if (hit == null) break;
			var success = ContextCache.TryRemove(hit, out _);
			if (!success) break;
		}

		return result;
	}

	public static async Task<string> SignAndCompactAsync(this ASActivity activity, UserKeypair keypair)
	{
		var expanded = Expand(activity) ?? throw new Exception("Failed to expand activity");
		var signed = await LdSignature.SignAsync(expanded, keypair.PrivateKey,
		                                         activity.Actor?.PublicKey?.Id ?? $"{activity.Actor!.Id}#main-key") ??
		             throw new Exception("Failed to sign activity");
		var compacted = Compact(signed) ?? throw new Exception("Failed to compact signed activity");
		var payload   = JsonConvert.SerializeObject(compacted, JsonSerializerSettings);

		return payload;
	}

	public static string Compact(this ASActivity activity)
	{
		var compacted = Compact((object)activity) ?? throw new Exception("Failed to compact signed activity");
		var payload   = JsonConvert.SerializeObject(compacted, JsonSerializerSettings);

		return payload;
	}

	public static JObject? Compact(object obj)
	{
		return Compact(JToken.FromObject(obj, JsonSerializer));
	}

	public static JArray? Expand(object obj)
	{
		return Expand(JToken.FromObject(obj, JsonSerializer));
	}

	public static JObject? Compact(JToken? json)
	{
		return JsonLdProcessor.Compact(json, DefaultContext, Options);
	}

	public static JArray? Expand(JToken? json)
	{
		return JsonLdProcessor.Expand(json, Options);
	}

	public static string Canonicalize(JArray json)
	{
		return JsonLdProcessor.Canonicalize(json);
	}

	public static string Canonicalize(JObject json)
	{
		return JsonLdProcessor.Canonicalize([json]);
	}
}