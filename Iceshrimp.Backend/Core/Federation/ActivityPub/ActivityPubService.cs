using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

//TODO: required attribute doesn't work with Newtonsoft.Json it appears
//TODO: enforce @type values

public class ActivityPubService(HttpClient client, HttpRequestService httpRqSvc) {
	private async Task<JArray> FetchActivity(string url) {
		var request  = httpRqSvc.Get(url, ["application/activity+json"]);
		var response = await client.SendAsync(request);
		var input    = await response.Content.ReadAsStringAsync();
		var json     = JsonConvert.DeserializeObject<JObject?>(input);

		var res = LDHelpers.Expand(json);
		if (res == null) throw new Exception("Failed to expand JSON-LD object");
		return res;
	}

	public async Task<ASActor> FetchActor(string uri) {
		var activity = await FetchActivity(uri);
		var actor    = activity.ToObject<List<ASActor>>();
		return actor?.First() ?? throw new Exception("Failed to fetch actor");
	}
}