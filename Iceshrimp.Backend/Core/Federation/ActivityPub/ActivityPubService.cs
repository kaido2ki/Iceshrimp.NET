using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

//TODO: required attribute doesn't work with Newtonsoft.Json it appears
//TODO: enforce @type values

public class ActivityPubService(HttpClient client, HttpRequestService httpRqSvc, ILogger<ActivityPubService> logger) {
	private static readonly JsonSerializerSettings JsonSerializerSettings =
		new() { DateParseHandling = DateParseHandling.None };

	public async Task<IEnumerable<ASObject>> FetchActivity(string url, User actor, UserKeypair keypair) {
		var request  = httpRqSvc.GetSigned(url, ["application/activity+json"], actor, keypair);
		var response = await client.SendAsync(request);
		var input    = await response.Content.ReadAsStringAsync();
		var json     = JsonConvert.DeserializeObject<JObject?>(input, JsonSerializerSettings);

		var res = LdHelpers.Expand(json) ?? throw new CustomException("Failed to expand JSON-LD object", logger);
		return res.Select(p => p.ToObject<ASObject>(new JsonSerializer { Converters = { new ASObjectConverter() } }) ??
		                       throw new CustomException("Failed to deserialize activity", logger));
	}

	public async Task<ASActor> FetchActor(string uri, User actor, UserKeypair keypair) {
		var activity = await FetchActivity(uri, actor, keypair);
		return activity.OfType<ASActor>().FirstOrDefault() ??
		       throw new CustomException("Failed to fetch actor", logger);
	}

	public async Task<ASNote> FetchNote(string uri, User actor, UserKeypair keypair) {
		var activity = await FetchActivity(uri, actor, keypair);
		return activity.OfType<ASNote>().FirstOrDefault() ?? throw new CustomException("Failed to fetch note", logger);
	}
}