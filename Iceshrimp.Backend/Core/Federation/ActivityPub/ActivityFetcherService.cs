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

public class ActivityFetcherService(HttpClient client, HttpRequestService httpRqSvc) {
	private static readonly JsonSerializerSettings JsonSerializerSettings =
		new();
	//FIXME: not doing this breaks ld signatures, but doing this breaks mapping the object to datetime properties
	//new() { DateParseHandling = DateParseHandling.None };

	public async Task<IEnumerable<ASObject>> FetchActivityAsync(string url, User actor, UserKeypair keypair) {
		var request  = httpRqSvc.GetSigned(url, ["application/activity+json"], actor, keypair);
		var response = await client.SendAsync(request);
		var input    = await response.Content.ReadAsStringAsync();
		var json     = JsonConvert.DeserializeObject<JObject?>(input, JsonSerializerSettings);

		var res = LdHelpers.Expand(json) ?? throw new GracefulException("Failed to expand JSON-LD object");
		return res.Select(p => p.ToObject<ASObject>(new JsonSerializer { Converters = { new ASObjectConverter() } }) ??
		                       throw new GracefulException("Failed to deserialize activity"));
	}

	public async Task<ASActor> FetchActorAsync(string uri, User actor, UserKeypair keypair) {
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASActor>().FirstOrDefault() ??
		       throw new GracefulException("Failed to fetch actor");
	}

	public async Task<ASNote?> FetchNoteAsync(string uri, User actor, UserKeypair keypair) {
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASNote>().FirstOrDefault();
	}
}