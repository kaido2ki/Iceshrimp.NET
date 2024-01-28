using System.Net;
using System.Text.Encodings.Web;
using System.Xml;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;

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
//FIXME: also check if the query references the local instance in other ways (e.g. @user@{WebDomain}, @user@{AccountDomain}, https://{WebDomain}/..., etc)

public class WebFingerService(HttpClient client, HttpRequestService httpRqSvc) {
	public async Task<WebFingerResponse?> ResolveAsync(string query) {
		(query, var proto, var domain) = ParseQuery(query);
		var webFingerUrl = GetWebFingerUrl(query, proto, domain);

		var req = httpRqSvc.Get(webFingerUrl, ["application/jrd+json", "application/json"]);
		var res = await client.SendAsync(req);

		return await res.Content.ReadFromJsonAsync<WebFingerResponse>();
	}

	private (string query, string proto, string domain) ParseQuery(string query) {
		string domain;
		string proto;
		query = query.StartsWith("acct:") ? $"@{query[5..]}" : query;
		if (query.StartsWith("http://") || query.StartsWith("https://")) {
			var uri = new Uri(query);
			domain = uri.Host;
			proto  = query.StartsWith("http://") ? "http" : "https";
		}
		else if (query.StartsWith('@')) {
			proto = "https";

			var split = query.Split('@');
			domain = split.Length switch {
				< 2 or > 3 => throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query"),
				2 => throw new GracefulException(HttpStatusCode.BadRequest, "Can't run WebFinger for local user"),
				_ => split[2]
			};
		}
		else {
			throw new GracefulException(HttpStatusCode.BadRequest, "Invalid query");
		}

		return (query, proto, domain);
	}

	private string GetWebFingerUrl(string query, string proto, string domain) {
		var template = GetWebFingerTemplateFromHostMeta($"{proto}://{domain}/.well-known/host-meta") ??
		               $"{proto}://{domain}/.well-known/webfinger?resource={{uri}}";
		var finalQuery = query.StartsWith('@') ? $"acct:{query[1..]}" : query;
		var encoded    = UrlEncoder.Default.Encode(finalQuery);
		return template.Replace("{uri}", encoded);
	}

	private string? GetWebFingerTemplateFromHostMeta(string hostMetaUrl) {
		var res = client.SendAsync(httpRqSvc.Get(hostMetaUrl, ["application/xrd+xml"]));
		var xml = new XmlDocument();
		xml.Load(res.Result.Content.ReadAsStreamAsync().Result);
		var section = xml["XRD"]?.GetElementsByTagName("Link");
		if (section == null) return null;

		//TODO: implement https://stackoverflow.com/a/37322614/18402176 instead

		for (var i = 0; i < section.Count; i++)
			if (section[i]?.Attributes?["rel"]?.InnerText == "lrdd")
				return section[i]?.Attributes?["template"]?.InnerText;

		return null;
	}
}