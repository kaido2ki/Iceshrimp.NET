using System.Collections;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VDS.RDF;
using VDS.RDF.JsonLd;
using VDS.RDF.Parsing;
using VDS.RDF.Writing.Formatting;

namespace Iceshrimp.Backend.Core.Federation.ActivityStreams;

public static class LDHelpers {
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

	private static RemoteDocument CustomLoader(Uri uri, JsonLdLoaderOptions jsonLdLoaderOptions) {
		//TODO: cache in redis
		RemoteDocument? result;
		ContextCache.TryGetValue(uri.ToString(), out result);
		if (result != null) return result;
		result = DefaultDocumentLoader.LoadJson(uri, jsonLdLoaderOptions);
		ContextCache.Add(uri.ToString(), result);

		return result;
	}

	private static string NormalizeForSignature(IEnumerable input) {
		//TODO: Find a way to convert JSON-LD to RDF directly
		//TODO: Fix XMLSchema#string thingy /properly/
		var store = new TripleStore();
		new JsonLdParser(new JsonLdProcessorOptions { DocumentLoader = CustomLoader, PruneBlankNodeIdentifiers = true })
			.Load(store, new StringReader(JsonConvert.SerializeObject(input)));
		//Console.WriteLine(store.Triples.SkipLast(1).Last().Object);

		var formatter = new NQuadsFormatter(NQuadsSyntax.Rdf11);
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		var ordered = store.Triples
		                   .Select(p => formatter.Format(p).Replace("^^<http://www.w3.org/2001/XMLSchema#string>", ""))
		                   .Order().ToList();
		var processedP1 = ordered.Where(p => p.StartsWith('_')).ToList();
		var processedP2 = ordered.Except(processedP1);

		return string.Join('\n', processedP2.Union(processedP1));
	}

	public static JArray? Expand(JToken? json) {
		return JsonLdProcessor.Expand(json, Options);
	}

	public static JObject? Compact(JToken? json) {
		return JsonLdProcessor.Compact(json, DefaultContext, Options);
	}

	public static JObject? Compact(object obj) {
		return Compact(JToken.FromObject(obj));
	}
}