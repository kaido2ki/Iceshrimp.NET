﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

// ReSharper disable UnusedMember.Local
// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable ConvertToAutoProperty
// ReSharper disable PropertyCanBeMadeInitOnly.Local

namespace Iceshrimp.Build.Tasks;

public class RewriteStaticAssetManifest : Microsoft.Build.Utilities.Task
{
	[System.ComponentModel.DataAnnotations.Required]
	public required ITaskItem Manifest { get; set; }

	public override bool Execute()
	{
		var parsed = Parse(Manifest.ItemSpec);
		var @fixed = Fixup(Manifest.ItemSpec, parsed);
		Write(Manifest.ItemSpec, @fixed);
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
		                           .DistinctBy(p => p.AssetPath)
		                           .ToDictionary(p => p.AssetPath,
		                                         p => p.ResponseHeaders
		                                               .FirstOrDefault(i => i.Name == "Content-Length"));

		var arr = manifest.Endpoints.ToArray();

		// Rewrite uncompressed versions to reference brotli-compressed asset instead
		foreach (var endpoint in arr)
		{
			if (endpoint.Selectors.Count > 0) continue;
			if (!brotliRoutes.TryGetValue(endpoint.AssetPath + ".br", out var len))
				continue;

			if (len is null) throw new Exception($"Couldn't find content-length for route ${endpoint.Route}");
			var origLen = endpoint.ResponseHeaders.First(p => p.Name == len.Name);
			endpoint.Properties.Add(new StaticAssetProperty("Uncompressed-Length", origLen.Value));
			endpoint.ResponseHeaders.Remove(origLen);
			endpoint.ResponseHeaders.Add(len);
			endpoint.AssetPath += ".br";
		}

		// Fixup assets without a corresponding compressed selector entry
		foreach (var endpoint in arr)
		{
			if (endpoint.Route == endpoint.AssetPath) continue;
			if (endpoint.Selectors is not []) continue;
			if (!endpoint.AssetPath.EndsWith(".br")) continue;
			if (endpoint.Route.EndsWith(".br")) continue;
			if (endpoint.ResponseHeaders.FirstOrDefault(p => p.Name == "Content-Length") is not { } lenHdr)
				continue;
			if (!long.TryParse(lenHdr.Value, out var len))
				continue;
			if (arr.Any(p => p.Route == endpoint.Route && p.AssetPath == endpoint.AssetPath && endpoint.Selectors is not []))
				continue;

			var quality = Math.Round(1.0 / (len + 1), 12).ToString("F12", CultureInfo.InvariantCulture);

			manifest.Endpoints.Add(new StaticAssetDescriptor
			{
				Route           = endpoint.Route,
				AssetPath       = endpoint.AssetPath,
				Properties      = endpoint.Properties,
				ResponseHeaders = [..endpoint.ResponseHeaders, new StaticAssetResponseHeader("Content-Encoding", "br")],
				Selectors       = [new StaticAssetSelector("Content-Encoding", "br", quality)]
			});
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
	}

	private sealed class StaticAssetDescriptor
	{
		private string?                         _route;
		private string?                         _assetFile;
		private List<StaticAssetSelector>       _selectors          = [];
		private List<StaticAssetProperty>       _endpointProperties = [];
		private List<StaticAssetResponseHeader> _responseHeaders    = [];

		public required string Route
		{
			get => _route ?? throw new InvalidOperationException("Route is required");
			set => _route = value;
		}

		[JsonPropertyName("AssetFile")]
		public required string AssetPath
		{
			get => _assetFile ?? throw new InvalidOperationException("AssetPath is required");
			set => _assetFile = value;
		}

		[JsonPropertyName("Selectors")]
		public List<StaticAssetSelector> Selectors
		{
			get => _selectors;
			set => _selectors = value;
		}

		[JsonPropertyName("EndpointProperties")]
		public List<StaticAssetProperty> Properties
		{
			get => _endpointProperties;
			set => _endpointProperties = value;
		}

		[JsonPropertyName("ResponseHeaders")]
		public List<StaticAssetResponseHeader> ResponseHeaders
		{
			get => _responseHeaders;
			set => _responseHeaders = value;
		}
	}

	private sealed class StaticAssetSelector(string name, string value, string quality)
	{
		public string Name    { get; } = name;
		public string Value   { get; } = value;
		public string Quality { get; } = quality;
	}

	private sealed class StaticAssetProperty(string name, string value)
	{
		public string Name  { get; } = name;
		public string Value { get; } = value;
	}

	private sealed class StaticAssetResponseHeader(string name, string value)
	{
		public string Name  { get; } = name;
		public string Value { get; } = value;
	}
}
