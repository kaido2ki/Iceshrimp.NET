using System.Net;
using System.Net.Http.Headers;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class ActivityFetcherService(
	IOptions<Config.InstanceSection> config,
	HttpClient client,
	HttpRequestService httpRqSvc,
	SystemUserService systemUserSvc,
	DatabaseContext db,
	ILogger<ActivityFetcherService> logger,
	FederationControlService fedCtrlSvc
)
{
	private static readonly IReadOnlyCollection<string> AcceptableActivityTypes =
	[
		"application/activity+json", "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
	];

	public async Task<IEnumerable<ASObject>> FetchActivityAsync(string url)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		return await FetchActivityAsync(url, actor, keypair);
	}

	public async Task<string?> FetchRawActivityAsync(string url)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		return await FetchRawActivityAsync(url, actor, keypair);
	}

	private async Task<IEnumerable<ASObject>> FetchActivityAsync(string url, User actor, UserKeypair keypair)
	{
		logger.LogDebug("Fetching activity {url} as user {id}", url, actor.Id);
		var (activity, finalUri) = await FetchActivityInternal(url, actor, keypair);
		if (activity == null) return [];

		var activityUri = new Uri(activity.Id);

		if (activityUri.ToString() == finalUri.ToString())
			return [activity];

		var activityIdUri = new Uri(activity.Id);

		if (activityIdUri.Host != finalUri.Host)
			throw GracefulException.UnprocessableEntity("Activity identifier doesn't match final host");

		logger.LogDebug("Fetching activity {url} as user {id} (attempt 2)", activityIdUri.AbsoluteUri, actor.Id);
		(activity, finalUri) = await FetchActivityInternal(activityIdUri.AbsoluteUri, actor, keypair);
		if (activity == null) return [];

		activityUri = new Uri(activity.Id);

		if (activityUri.ToString() == finalUri.ToString())
			return [activity];

		throw GracefulException
			.UnprocessableEntity("Activity identifier still doesn't match final URL after second fetch attempt");
	}

	private async Task<(ASObject? obj, Uri finalUri)> FetchActivityInternal(
		string url, User actor, UserKeypair keypair, int recurse = 3
	)
	{
		var requestHost = new Uri(url).Host;
		if (requestHost == config.Value.WebDomain || requestHost == config.Value.AccountDomain)
			throw GracefulException.UnprocessableEntity("Refusing to fetch activity from local domain");
		
		if (await fedCtrlSvc.ShouldBlockAsync(requestHost))
		{
			logger.LogDebug("Refusing to fetch activity from blocked instance");
			return (null, new Uri(url));
		}

		var request  = httpRqSvc.GetSigned(url, AcceptableActivityTypes, actor, keypair);
		var response = await client.SendAsync(request);

		if (IsRedirect(response))
		{
			var location = response.Headers.Location;
			if (location == null) throw new Exception("Redirection requested but no location header found");
			if (recurse <= 0) throw new Exception("Redirection requested but recurse counter is at zero");
			return await FetchActivityInternal(location.ToString(), actor, keypair, --recurse);
		}

		var finalUri = response.RequestMessage?.RequestUri ??
		               throw new Exception("RequestMessage must not be null at this stage");

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == HttpStatusCode.Gone)
				throw AuthFetchException.NotFound("The remote user no longer exists.");
			logger.LogDebug("Failed to fetch activity: response status was {code}", response.StatusCode);
			return (null, finalUri);
		}

		if (!IsValidActivityContentType(response.Content.Headers.ContentType))
		{
			logger.LogDebug("Failed to fetch activity: content type {type} is invalid",
			                response.Content.Headers.ContentType);
			return (null, finalUri);
		}

		var input = await response.Content.ReadAsStringAsync();
		var json  = JsonConvert.DeserializeObject<JObject?>(input);

		var res = LdHelpers.Expand(json) ?? throw new GracefulException("Failed to expand JSON-LD object");
		if (res.Count != 1)
			throw new GracefulException("Received zero or more than one activity");

		var activity = res[0].ToObject<ASObject>(new JsonSerializer { Converters = { new ASObjectConverter() } }) ??
		               throw new GracefulException("Failed to deserialize activity");

		if (finalUri.Host == config.Value.WebDomain || finalUri.Host == config.Value.WebDomain)
			throw GracefulException.UnprocessableEntity("Refusing to process activity from local domain");

		return (activity, finalUri);
	}

	private static bool IsRedirect(HttpResponseMessage response)
	{
		switch (response.StatusCode)
		{
			case HttpStatusCode.MultipleChoices:
			case HttpStatusCode.Moved:
			case HttpStatusCode.Found:
			case HttpStatusCode.SeeOther:
			case HttpStatusCode.TemporaryRedirect:
			case HttpStatusCode.PermanentRedirect:
				return true;

			default:
				return false;
		}
	}

	private async Task<string?> FetchRawActivityAsync(string url, User actor, UserKeypair keypair)
	{
		var request  = httpRqSvc.GetSigned(url, AcceptableActivityTypes, actor, keypair).DisableAutoRedirects();
		var response = await client.SendAsync(request);

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == HttpStatusCode.Gone)
				throw AuthFetchException.NotFound("The remote user no longer exists.");
			logger.LogDebug("Failed to fetch activity: response status was {code}", response.StatusCode);
			return null;
		}

		if (!IsValidActivityContentType(response.Content.Headers.ContentType))
		{
			logger.LogDebug("Failed to fetch activity: content type {type} is invalid",
			                response.Content.Headers.ContentType);
			return null;
		}

		return await response.Content.ReadAsStringAsync();
	}

	public static bool IsValidActivityContentType(MediaTypeHeaderValue? headersContentType) =>
		headersContentType switch
		{
			{ MediaType: "application/activity+json" } => true,
			{ MediaType: "application/ld+json" } when headersContentType.Parameters.Any(p =>
					p.Value != null &&
					p.Name.ToLowerInvariant() == "profile" &&
					p.Value.Split(" ").Contains("\"https://www.w3.org/ns/activitystreams\""))
				=> true,
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

	private async Task<ASNote?> FetchNoteAsync(string uri, User actor, UserKeypair keypair)
	{
		var activity = await FetchActivityAsync(uri, actor, keypair);
		var note     = activity.OfType<ASNote>().FirstOrDefault();
		if (note != null)
			note.VerifiedFetch = true;
		return note;
	}

	public async Task<ASNote?> FetchNoteAsync(string uri, User actor)
	{
		var keypair = await db.UserKeypairs.FirstOrDefaultAsync(p => p.User == actor) ??
		              throw new Exception("Actor has no keypair");
		return await FetchNoteAsync(uri, actor, keypair);
	}

	public async Task<ASNote?> FetchNoteAsync(string uri)
	{
		var (actor, keypair) = await systemUserSvc.GetInstanceActorWithKeypairAsync();
		return await FetchNoteAsync(uri, actor, keypair);
	}
}