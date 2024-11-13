using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

// ReSharper disable once CheckNamespace
namespace Iceshrimp.Build.Tasks;

public class RewriteStaticAssetManifest : Microsoft.Build.Utilities.Task
{
	public static void FixupFile(string manifestPath)
	{
		var parsed = Parse(manifestPath);
		var @fixed = Fixup(manifestPath, parsed);
		Write(manifestPath, @fixed);
	}

	[System.ComponentModel.DataAnnotations.Required]
	public required ITaskItem[] ManifestFiles { get; set; }

	public override bool Execute()
	{
		foreach (var item in ManifestFiles) FixupFile(item.ItemSpec);
		return true;
	}

	private static StaticAssetsManifest Parse(string manifestPath)
	{
		using var stream  = File.OpenRead(manifestPath);
		using var reader  = new StreamReader(stream);
		var       content = reader.ReadToEnd();

		// @formatter:off
		var result = JsonSerializer.Deserialize<StaticAssetsManifest>(content) ??
		             throw new InvalidOperationException($"The static resources manifest file '{manifestPath}' could not be deserialized.");
		// @formatter:on

		return result;
	}

	private static void Write(string manifestPath, StaticAssetsManifest manifest)
	{
		File.Delete(manifestPath);
		using var stream = File.OpenWrite(manifestPath);
		JsonSerializer.Serialize(stream, manifest, new JsonSerializerOptions { WriteIndented = true });
	}

	private static StaticAssetsManifest Fixup(string manifestPath, StaticAssetsManifest manifest)
	{
		// Get a list of constrained routes
		var brotliRoutes = manifest.Endpoints
		                           .Where(p => p.Selectors is [{ Name: "Content-Encoding", Value: "br" }])
		                           .ToDictionary(p => p.Route,
		                                         p => p.ResponseHeaders
		                                               .FirstOrDefault(i => i.Name == "Content-Length"));

		// Remove gzip-compressed versions
		foreach (var endpoint in manifest.Endpoints.ToArray())
		{
			if (!endpoint.Selectors.Any(p => p is { Name: "Content-Encoding", Value: "gzip" }))
				continue;

			manifest.Endpoints.Remove(endpoint);
		}

		// Rewrite uncompressed versions to reference brotli-compressed asset instead
		foreach (var endpoint in manifest.Endpoints.ToArray())
		{
			if (endpoint.Selectors.Count > 0) continue;
			if (!brotliRoutes.TryGetValue(endpoint.AssetPath, out var len)) continue;
			if (len is null) throw new Exception($"Couldn't find content-length for route ${endpoint.Route}");
			var origLen = endpoint.ResponseHeaders.First(p => p.Name == len.Name);
			endpoint.Properties.Add(new StaticAssetProperty("Uncompressed-Length", origLen.Value));
			endpoint.ResponseHeaders.Remove(origLen);
			endpoint.ResponseHeaders.Add(len);
			endpoint.AssetPath += ".br";
		}
		
		// Remove explicit routes
		manifest.Endpoints.RemoveAll(p => p.Route.EndsWith(".br"));

		// Clean up endpoints
		var path = Path.GetDirectoryName(manifestPath) ?? throw new Exception("Invalid path");
		manifest.Endpoints.RemoveAll(p => !File.Exists(Path.Combine(path, "wwwroot", p.AssetPath)));
		return manifest;
	}

	private class StaticAssetsManifest
	{
		public int Version { get; set; }

		public string ManifestType { get; set; } = "";

		// ReSharper disable once CollectionNeverUpdated.Local
		public List<StaticAssetDescriptor> Endpoints { get; set; } = [];

		public bool IsBuildManifest() => string.Equals(ManifestType, "Build", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// The description of a static asset that was generated during the build process.
	/// </summary>
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	public sealed class StaticAssetDescriptor
	{
		private string?                         _route;
		private string?                         _assetFile;
		private List<StaticAssetSelector>       _selectors          = [];
		private List<StaticAssetProperty>       _endpointProperties = [];
		private List<StaticAssetResponseHeader> _responseHeaders    = [];

		/// <summary>
		/// The route that the asset is served from.
		/// </summary>
		public required string Route
		{
			get => _route ?? throw new InvalidOperationException("Route is required");
			set => _route = value;
		}

		/// <summary>
		/// The path to the asset file from the wwwroot folder.
		/// </summary>
		[JsonPropertyName("AssetFile")]
		public required string AssetPath
		{
			get => _assetFile ?? throw new InvalidOperationException("AssetPath is required");
			set => _assetFile = value;
		}

		/// <summary>
		/// A list of selectors that are used to discriminate between two or more assets with the same route.
		/// </summary>
		[JsonPropertyName("Selectors")]
		public List<StaticAssetSelector> Selectors
		{
			get => _selectors;
			set => _selectors = value;
		}

		/// <summary>
		/// A list of properties that are associated with the endpoint.
		/// </summary>
		[JsonPropertyName("EndpointProperties")]
		public List<StaticAssetProperty> Properties
		{
			get => _endpointProperties;
			set => _endpointProperties = value;
		}

		/// <summary>
		/// A list of headers to apply to the response when this resource is served.
		/// </summary>
		[JsonPropertyName("ResponseHeaders")]
		public List<StaticAssetResponseHeader> ResponseHeaders
		{
			get => _responseHeaders;
			set => _responseHeaders = value;
		}

		private string GetDebuggerDisplay()
		{
			return $"Route: {Route} Path: {AssetPath}";
		}
	}

	/// <summary>
	/// A static asset selector. Selectors are used to discriminate between two or more assets with the same route.
	/// </summary>
	/// <param name="name">The name associated to the selector.</param>
	/// <param name="value">The value associated to the selector and used to match against incoming requests.</param>
	/// <param name="quality">The static server quality associated to this selector.</param>
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	public sealed class StaticAssetSelector(string name, string value, string quality)
	{
		/// <summary>
		/// The name associated to the selector.
		/// </summary>
		public string Name { get; } = name;

		/// <summary>
		/// The value associated to the selector and used to match against incoming requests.
		/// </summary>
		public string Value { get; } = value;

		/// <summary>
		/// The static asset server quality associated to this selector. Used to break ties when a request matches multiple values
		/// with the same degree of specificity.
		/// </summary>
		public string Quality { get; } = quality;

		private string GetDebuggerDisplay() => $"Name: {Name} Value: {Value} Quality: {Quality}";
	}

	/// <summary>
	/// A property associated with a static asset.
	/// </summary>
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	public sealed class StaticAssetProperty(string name, string value)
	{
		/// <summary>
		/// The name of the property.
		/// </summary>
		public string Name { get; } = name;

		/// <summary>
		/// The value of the property.
		/// </summary>
		public string Value { get; } = value;

		private string GetDebuggerDisplay() => $"Name: {Name} Value:{Value}";
	}

	/// <summary>
	/// A response header to apply to the response when a static asset is served.
	/// </summary>
	/// <param name="name">The name of the header.</param>
	/// <param name="value">The value of the header.</param>
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	public sealed class StaticAssetResponseHeader(string name, string value)
	{
		/// <summary>
		/// The name of the header.
		/// </summary>
		public string Name { get; } = name;

		/// <summary>
		/// The value of the header.
		/// </summary>
		public string Value { get; } = value;

		private string GetDebuggerDisplay() => $"Name: {Name} Value: {Value}";
	}
}