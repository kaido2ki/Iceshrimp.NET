using System.Collections.Immutable;
using System.Net;
using System.Text.Encodings.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using Iceshrimp.Backend.Controllers.Federation.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

/*
 * There's two different WebFinger implementations out there
 * 1. Get /.well-known/host-meta, extract the WebFinger query url template from there
 * 2. Get /.well-known/webfinger?resource={uri} directly
 *
 * We have to check for host-meta first, and only fall back to the second implementation if it
 * - doesn't exist
 * - doesn't have a query url template
 */

//FIXME: handle cursed person/group acct collisions like https://lemmy.ml/.well-known/webfinger?resource=acct:linux@lemmy.ml

public class WebFingerService(
	HttpClient client,
	HttpRequestService httpRqSvc,
	IHostApplicationLifetime appLifetime,
	IOptions<Config.InstanceSection> config
)
{
	private static readonly ImmutableArray<string> Accept =
	[
		"application/jrd+json", "application/json", "application/xrd+xml", "application/xml"
	];

	public async Task<WebFingerResponse?> ResolveAsync(string query)
	{
		(query, var proto, var domain) = ParseQuery(query);
		if (domain == config.Value.WebDomain || domain == config.Value.AccountDomain)
			throw new GracefulException(HttpStatusCode.BadRequest, "Can't run WebFinger for local user");

		var webFingerUrl = await GetWebFingerUrlAsync(query, proto, domain);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(appLifetime.ApplicationStopping);
		cts.CancelAfter(TimeSpan.FromSeconds(10));

		var req = httpRqSvc.Get(webFingerUrl, Accept);
		var res = await client.SendAsync(req, cts.Token);

		if (res.StatusCode == HttpStatusCode.Gone)
			throw AuthFetchException.NotFound("The remote user no longer exists.");
		if (!res.IsSuccessStatusCode)
			return null;
		if (!Accept.Contains(res.Content.Headers.ContentType?.MediaType ?? ""))
			return null;

		if (res.Content.Headers.ContentType?.MediaType is "application/jrd+json" or "application/json")
			return await res.Content.ReadFromJsonAsync<WebFingerResponse>(cts.Token);

		var deserializer = new XmlSerializer(typeof(WebFingerResponse));

		return deserializer.Deserialize(await res.Content.ReadAsStreamAsync(cts.Token)) as WebFingerResponse ??
		       throw new Exception("Failed to deserialize xml payload");
	}

	public static (string query, string proto, string domain) ParseQuery(string query)
	{
		string domain;
		string proto;
		query = query.StartsWith("acct:") ? $"@{query[5..]}" : query;
		if (query.StartsWith("http://") || query.StartsWith("https://"))
		{
			var uri = new Uri(query);
			domain = uri.Host;
			proto  = query.StartsWith("http://") ? "http" : "https";
		}
		else if (query.StartsWith('@'))
		{
			proto = "https";
			var split = query.Split('@');

			// @formatter:off
			domain = split.Length switch
			{
				< 2 or > 3 => throw new GracefulException(HttpStatusCode.BadRequest, $"Invalid query: {query}"),
				2          => throw new GracefulException(HttpStatusCode.BadRequest, $"Can't run WebFinger for local user: {query}"),
				_          => split[2]
			};
			// @formatter:on
		}
		else
		{
			throw new GracefulException(HttpStatusCode.BadRequest, $"Invalid query: {query}");
		}

		return (query, proto, domain);
	}

	private async Task<string> GetWebFingerUrlAsync(string query, string proto, string domain)
	{
		var template = await GetWebFingerTemplateFromHostMetaXmlAsync(proto, domain) ??
		               await GetWebFingerTemplateFromHostMetaJsonAsync(proto, domain) ??
		               $"{proto}://{domain}/.well-known/webfinger?resource={{uri}}";

		var finalQuery = query.StartsWith('@') ? $"acct:{query[1..]}" : query;
		var encoded    = UrlEncoder.Default.Encode(finalQuery);
		return template.Replace("{uri}", encoded);
	}

	// Technically, we should be checking for rel=lrdd *and* type=application/jrd+json, but nearly all implementations break this, so we can't.
	private async Task<string?> GetWebFingerTemplateFromHostMetaXmlAsync(string proto, string domain)
	{
		try
		{
			var hostMetaUrl = $"{proto}://{domain}/.well-known/host-meta";
			using var res = await client.SendAsync(httpRqSvc.Get(hostMetaUrl, ["application/xrd+xml"]),
			                                       HttpCompletionOption.ResponseHeadersRead);
			await using var stream = await res.Content.ReadAsStreamAsync();

			return XElement.Load(stream)
			               .Descendants(XName.Get("Link", "http://docs.oasis-open.org/ns/xri/xrd-1.0"))
			               //.Where(p => Accept.Contains(p.Attribute("type"))?.Value ?? ""))
			               .FirstOrDefault(p => p.Attribute("rel")?.Value == "lrdd")
			               ?.Attribute("template")
			               ?.Value;
		}
		catch
		{
			return null;
		}
	}

	// See above comment as for why jrd+json is commented out.
	private async Task<string?> GetWebFingerTemplateFromHostMetaJsonAsync(string proto, string domain)
	{
		try
		{
			var hostMetaUrl = $"{proto}://{domain}/.well-known/host-meta.json";
			using var res = await client.SendAsync(httpRqSvc.Get(hostMetaUrl, ["application/jrd+json"]),
			                                       HttpCompletionOption.ResponseHeadersRead);
			var deserialized = await res.Content.ReadFromJsonAsync<HostMetaJsonResponse>();

			var result = deserialized?.Links?.FirstOrDefault(p => p is
			{
				Rel: "lrdd",
				//Type: "application/jrd+json",
				Template: not null
			});

			if (result?.Template != null)
				return result.Template;
		}
		catch
		{
			// ignored
		}

		try
		{
			var hostMetaUrl = $"{proto}://{domain}/.well-known/host-meta";
			using var res = await client.SendAsync(httpRqSvc.Get(hostMetaUrl, ["application/jrd+json"]),
			                                       HttpCompletionOption.ResponseHeadersRead);
			var deserialized = await res.Content.ReadFromJsonAsync<HostMetaJsonResponse>();

			var result = deserialized?.Links?.FirstOrDefault(p => p is
			{
				Rel: "lrdd",
				//Type: "application/jrd+json",
				Template: not null
			});

			return result?.Template;
		}
		catch
		{
			// ignored
		}

		return null;
	}
}