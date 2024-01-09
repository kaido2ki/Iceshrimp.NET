using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Xml;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Helpers;

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

public class WebFinger {
	private readonly string  _query;
	private readonly string  _proto;
	private readonly string  _domain;
	private readonly string? _username;
	private          string? _webFingerUrl;

	private string HostMetaUrl              => $"{_proto}://{_domain}/.well-known/host-meta";
	private string DefaultWebFingerTemplate => $"{_proto}://{_domain}/.well-known/webfinger?resource={{uri}}";

	public WebFinger(string query) {
		_query = query;
		if (_query.StartsWith("http://") || _query.StartsWith("https://")) {
			var uri = new Uri(_query);
			_domain = uri.Host;
			_proto  = _query.StartsWith("http://") ? "http" : "https";
		}
		else if (_query.StartsWith('@')) {
			_proto = "https";

			var split = _query.Split('@');
			if (split.Length == 2) {
				throw new Exception("Can't run WebFinger for local user");
			}

			if (split.Length == 3) {
				_username = split[1];
				_domain   = split[2];
			}
			else {
				throw new Exception("Invalid query");
			}
		}
		else {
			throw new Exception("Invalid query");
		}
	}

	private string? GetWebFingerTemplateFromHostMeta() {
		var client = HttpClientHelpers.HttpClient;
		var request = new HttpRequestMessage {
			RequestUri = new Uri(HostMetaUrl),
			Method     = HttpMethod.Get,
			Headers    = { Accept = { MediaTypeWithQualityHeaderValue.Parse("application/xrd+xml") } }
		};
		var res = client.SendAsync(request);
		var xml = new XmlDocument();
		xml.Load(res.Result.Content.ReadAsStreamAsync().Result);
		var section = xml["XRD"]?.GetElementsByTagName("Link");
		if (section == null) return null;

		//TODO: implement https://stackoverflow.com/a/37322614/18402176 instead

		for (var i = 0; i < section.Count; i++) {
			if (section[i]?.Attributes?["rel"]?.InnerText == "lrdd") {
				return section[i]?.Attributes?["template"]?.InnerText;
			}
		}

		return null;
	}

	private string GetWebFingerUrl() {
		var template = GetWebFingerTemplateFromHostMeta() ?? DefaultWebFingerTemplate;
		var query    = _query.StartsWith('@') ? $"acct:{_query.Substring(1)}" : _query;
		var encoded  = UrlEncoder.Default.Encode(query);
		return template.Replace("{uri}", encoded);
	}

	public async Task<WebFingerResponse?> Resolve() {
		_webFingerUrl = GetWebFingerUrl();

		var client = HttpClientHelpers.HttpClient;
		var request = new HttpRequestMessage {
			RequestUri = new Uri(_webFingerUrl),
			Method     = HttpMethod.Get,
			Headers = {
				Accept = {
					MediaTypeWithQualityHeaderValue.Parse("application/jrd+json"),
					MediaTypeWithQualityHeaderValue.Parse("application/json")
				}
			}
		};
		var res = await client.SendAsync(request);
		return await res.Content.ReadFromJsonAsync<WebFingerResponse>();
	}
}