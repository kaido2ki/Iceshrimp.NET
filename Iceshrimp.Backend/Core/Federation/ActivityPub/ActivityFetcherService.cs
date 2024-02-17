using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityFetcherService(HttpClient client, HttpRequestService httpRqSvc, SystemUserService systemUserSvc)
{
	private static readonly IReadOnlyCollection<string> AcceptableActivityTypes =
	[
		"application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
	];

	private static readonly JsonSerializerSettings JsonSerializerSettings = new();
	//FIXME: not doing this breaks ld signatures, but doing this breaks mapping the object to datetime properties
	//new() { DateParseHandling = DateParseHandling.None };

	public async Task<IEnumerable<ASObject>> FetchActivityAsync(string url)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		return await FetchActivityAsync(url, actor, keypair);
	}

	public async Task<IEnumerable<ASObject>> FetchActivityAsync(string url, User actor, UserKeypair keypair)
	{
		var request  = httpRqSvc.GetSigned(url, AcceptableActivityTypes, actor, keypair);
		var response = await client.SendAsync(request);

		if (!response.IsSuccessStatusCode) return [];
		if (!IsValidActivityContentType(response.Content.Headers.ContentType))
			return [];

		var finalUri = response.RequestMessage?.RequestUri ??
		               throw new Exception("RequestMessage must not be null at this stage");

		var input = await response.Content.ReadAsStringAsync();
		var json  = JsonConvert.DeserializeObject<JObject?>(input, JsonSerializerSettings);

		var res = LdHelpers.Expand(json) ?? throw new GracefulException("Failed to expand JSON-LD object");
		var activities =
			res.Select(p => p.ToObject<ASObject>(new JsonSerializer { Converters = { new ASObjectConverter() } }) ??
			                throw new GracefulException("Failed to deserialize activity"))
			   .ToList();

		if (activities.Any(p => new Uri(p.Id).Host != finalUri.Host))
			throw new GracefulException("Activity identifier doesn't match final host");

		return activities;
	}

	private static bool IsValidActivityContentType(MediaTypeHeaderValue? headersContentType) =>
		headersContentType switch
		{
			{ MediaType: "application/activity+json" } => true,
			{ MediaType: "application/ld+json" } when headersContentType.Parameters.Any(p => p is
			{
				Name: "profile", Value: "https://www.w3.org/ns/activitystreams"
			}) => true,
			_ => false
		};

	public async Task<ASActor> FetchActorAsync(string uri, User actor, UserKeypair keypair)
	{
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASActor>().FirstOrDefault() ??
		       throw new GracefulException("Failed to fetch actor");
	}

	public async Task<ASActor> FetchActorAsync(string uri)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASActor>().FirstOrDefault() ??
		       throw new GracefulException("Failed to fetch actor");
	}

	public async Task<ASNote?> FetchNoteAsync(string uri, User actor, UserKeypair keypair)
	{
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASNote>().FirstOrDefault();
	}

	public async Task<ASNote?> FetchNoteAsync(string uri)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		var activity = await FetchActivityAsync(uri, actor, keypair);
		return activity.OfType<ASNote>().FirstOrDefault();
	}
}