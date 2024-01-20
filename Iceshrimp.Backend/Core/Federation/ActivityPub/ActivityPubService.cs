using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

//TODO: required attribute doesn't work with Newtonsoft.Json it appears
//TODO: enforce @type values

public class ActivityPubService(HttpClient client, HttpRequestService httpRqSvc) {
	private static readonly JsonSerializerSettings JsonSerializerSettings =
		new() { DateParseHandling = DateParseHandling.None };

	public async Task<IEnumerable<ASObject>> FetchActivity(string url) {
		var request  = httpRqSvc.Get(url, ["application/activity+json"]);
		var response = await client.SendAsync(request);
		var input    = await response.Content.ReadAsStringAsync();
		var json     = JsonConvert.DeserializeObject<JObject?>(input, JsonSerializerSettings);

		var res = LdHelpers.Expand(json) ?? throw new Exception("Failed to expand JSON-LD object");
		return res.Select(p => p.ToObject<ASObject>(new JsonSerializer { Converters = { new ASObjectConverter() } }) ??
		                       throw new Exception("Failed to deserialize activity"));
	}

	public async Task<ASActor> FetchActor(string uri) {
		var activity = await FetchActivity(uri);
		return activity.OfType<ASActor>().FirstOrDefault() ?? throw new Exception("Failed to fetch actor");
	}
}