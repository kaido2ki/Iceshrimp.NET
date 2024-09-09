using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserResolver(
	ILogger<UserResolver> logger,
	UserService userSvc,
	WebFingerService webFingerSvc,
	FollowupTaskService followupTaskSvc,
	ActivityFetcherService fetchSvc,
	IOptions<Config.InstanceSection> config,
	DatabaseContext db
)
{
	private static readonly AsyncKeyedLocker<string> KeyedLocker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	/*
	 * The full web finger algorithm:
	 *
	 * 1. WebFinger(input_uri), find the rel=self type=application/activity+json entry, that's ap_uri
	 * 1.1 Failing this, fetch the actor
	 * 1.1.1 If the actor uri differs from the query, recurse (once!) with the new uri
	 * 1.1.2 Otherwise, Perform reverse discovery for the actor uri
	 * 2. WebFinger(ap_uri), find the first acct: URI in [subject] + aliases, that's candidate_acct_uri
	 * 2.1 Failing this, perform reverse discovery for ap_uri
	 * 3. WebFinger(candidate_acct_uri), validate it also points to ap_uri. If so, you have acct_uri
	 * 3.1 Failing this, acct_uri = "acct:" + preferredUsername from AP actor + "@" + hostname from ap_uri
	 *
	 * Avoid repeating WebFinger's with same URI for performance, optimize away validation checks when the domain matches.
	 * Skip step 2 when performing reverse discovery & ap_uri matches the actor uri.
	 */

	private async Task<(string Acct, string Uri)> WebFingerAsync(
		string query, bool recurse = true, string? actorUri = null,
		Dictionary<string, WebFingerResponse>? responses = null
	)
	{
		if (actorUri == null)
			logger.LogDebug("Running WebFinger for query '{query}'", query);
		else
			logger.LogDebug("Performing WebFinger reverse discovery for query '{query}' and uri '{uri}'",
			                query, actorUri);

		responses ??= [];

		var fingerRes = await webFingerSvc.ResolveAsync(query);
		if (fingerRes == null)
		{
			if (recurse && query.StartsWith("https://"))
			{
				logger.LogDebug("WebFinger returned null, fetching actor as fallback");
				try
				{
					var actor = await fetchSvc.FetchActorAsync(query);
					if (query != actor.Id)
					{
						logger.LogDebug("Actor ID differs from query, retrying...");
						return await WebFingerAsync(actor.Id, false);
					}

					logger.LogDebug("Actor ID matches query, performing reverse discovery...");
					actor.Normalize(query);
					var domain   = new Uri(actor.Id).Host;
					var username = actor.Username!;
					return await WebFingerAsync(actor.WebfingerAddress ?? $"acct:{username}@{domain}", false, actor.Id);
				}
				catch (Exception e)
				{
					logger.LogDebug("Failed to fetch actor {uri}: {e}", query, e.Message);
				}
			}

			throw new GracefulException($"Failed to WebFinger '{query}'");
		}

		responses.Add(query, fingerRes);

		var apUri = fingerRes.Links
		                     .FirstOrDefault(p => p is { Rel: "self", Type: Constants.APMime or Constants.ASMime })
		                     ?.Href;

		if (apUri == null)
			throw new GracefulException($"WebFinger response for '{query}' didn't contain a candidate link");
		var subjectUri = GetAcctUri(fingerRes) ??
		                 throw new Exception($"WebFinger response for '{apUri}' didn't contain any acct uris");

		var queryHost   = WebFingerService.ParseQuery(query).domain;
		var subjectHost = WebFingerService.ParseQuery(subjectUri).domain;
		var apUriHost   = new Uri(apUri).Host;
		if (subjectHost == apUriHost && subjectHost == queryHost)
			return (subjectUri, apUri);

		// We need to skip this step when performing reverse discovery & the uris match
		if (apUri != actorUri)
		{
			if (actorUri != null) throw new Exception("Reverse discovery failed: uri mismatch");
			fingerRes = responses.GetValueOrDefault(apUri);
			if (fingerRes == null)
			{
				logger.LogDebug("AP uri didn't match query, re-running WebFinger for '{apUri}'", apUri);
				fingerRes = await webFingerSvc.ResolveAsync(apUri);
				if (fingerRes == null)
				{
					logger.LogDebug("Failed to validate apUri, falling back to reverse discovery");
					try
					{
						var actor = await fetchSvc.FetchActorAsync(apUri);
						if (apUri != actor.Id)
							throw new Exception("Reverse discovery fallback failed: uri mismatch");

						logger.LogDebug("Actor ID matches apUri, performing reverse discovery...");
						actor.Normalize(apUri);
						var domain   = new Uri(actor.Id).Host;
						var username = new Uri(actor.Username!).Host;
						return await WebFingerAsync(actor.WebfingerAddress ?? $"acct:{username}@{domain}", false,
						                            apUri, responses);
					}
					catch (Exception e)
					{
						logger.LogDebug("Failed to fetch actor {uri}: {e}", query, e.Message);
						throw new GracefulException($"Failed to WebFinger '{query}'");
					}
				}

				responses.Add(apUri, fingerRes);
			}
		}

		var acctUri = GetAcctUri(fingerRes) ??
		              throw new Exception($"WebFinger response for '{apUri}' didn't contain any acct uris");

		if (WebFingerService.ParseQuery(acctUri).domain == apUriHost)
			return (acctUri, apUri);

		fingerRes = responses.GetValueOrDefault(acctUri);
		if (fingerRes == null)
		{
			logger.LogDebug("Acct uri didn't match query, re-running WebFinger for '{acctUri}'", acctUri);

			fingerRes = await webFingerSvc.ResolveAsync(acctUri);

			if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{acctUri}'");
			responses.Add(acctUri, fingerRes);
		}

		var finalAcct = GetAcctUri(fingerRes) ??
		                throw new Exception($"WebFinger response for '{acctUri}' didn't contain any acct uris");
		var finalUri = fingerRes.Links.FirstOrDefault(p => p is { Rel: "self", Type: "application/activity+json" })
		                        ?.Href ??
		               throw new GracefulException("Final AP URI was null");

		if (apUri != finalUri)
		{
			logger.LogDebug("WebFinger: finalUri doesn't match apUri, setting acct host to apUri host: {apUri}", apUri);
			var split = finalAcct.Split('@');
			if (split.Length != 2)
				throw new GracefulException($"Failed to finalize WebFinger for '{apUri}': invalid acct '{finalAcct}'");
			split[1]  = new Uri(apUri).Host;
			finalAcct = string.Join('@', split);
		}

		return (finalAcct, finalUri);
	}

	private static string? GetAcctUri(WebFingerResponse fingerRes) => (fingerRes.Aliases ?? [])
	                                                                  .Prepend(fingerRes.Subject)
	                                                                  .FirstOrDefault(p => p.StartsWith("acct:"));

	public static string NormalizeQuery(string query)
	{
		if (query.StartsWith("https://") || query.StartsWith("http://"))
			if (query.Contains('#'))
				query = query.Split("#")[0];
			else
				return query;
		else if (query.StartsWith('@'))
			query = $"acct:{query[1..]}";
		else if (!query.StartsWith("acct:"))
			query = $"acct:{query}";

		return query;
	}

	public async Task<User> ResolveAsync(string username, string? host)
	{
		return host != null ? await ResolveAsync($"acct:{username}@{host}") : await ResolveAsync($"acct:{username}");
	}

	public async Task<User?> LookupAsync(string query)
	{
		query = NormalizeQuery(query);
		return await userSvc.GetUserFromQueryAsync(query);
	}

	public async Task<User> ResolveAsync(string query)
	{
		query = NormalizeQuery(query);

		// Before we begin, let's skip local note urls
		if (query.StartsWith($"https://{config.Value.WebDomain}/notes/"))
			throw GracefulException.BadRequest("Refusing to resolve local note URL as user");

		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQueryAsync(query);
		if (user != null) return user;

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFingerAsync(query);

		// Check the database again with the new data
		if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
		if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
		if (user != null) return user;

		using (await KeyedLocker.LockAsync(uri))
		{
			// Pass the job on to userSvc, which will create the user
			return await userSvc.CreateUserAsync(uri, acct);
		}
	}

	public async Task<User?> ResolveAsync(string query, bool onlyExisting)
	{
		query = NormalizeQuery(query);

		// Before we begin, let's skip local note urls
		if (query.StartsWith($"https://{config.Value.WebDomain}/notes/"))
			throw GracefulException.BadRequest("Refusing to resolve local note URL as user");

		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQueryAsync(query);
		if (user != null) return user;

		if (onlyExisting)
			return null;

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFingerAsync(query);

		// Check the database again with the new data
		if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
		if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
		if (user != null) return user;

		using (await KeyedLocker.LockAsync(uri))
		{
			// Pass the job on to userSvc, which will create the user
			return await userSvc.CreateUserAsync(uri, acct);
		}
	}

	public async Task<User?> ResolveAsyncOrNull(string username, string? host)
	{
		try
		{
			var query = $"acct:{username}@{host}";

			// First, let's see if we already know the user
			var user = await userSvc.GetUserFromQueryAsync(query);
			if (user != null) return user;

			if (host == null) return null;

			// We don't, so we need to run WebFinger
			var (acct, uri) = await WebFingerAsync(query);

			// Check the database again with the new data
			if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
			if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
			if (user != null) return user;

			using (await KeyedLocker.LockAsync(uri))
			{
				// Pass the job on to userSvc, which will create the user
				return await userSvc.CreateUserAsync(uri, acct);
			}
		}
		catch
		{
			return null;
		}
	}

	public async Task<User?> ResolveAsyncOrNull(string query)
	{
		try
		{
			query = NormalizeQuery(query);

			// First, let's see if we already know the user
			var user = await userSvc.GetUserFromQueryAsync(query);
			if (user != null) return user;

			if (query.StartsWith($"https://{config.Value.WebDomain}/")) return null;

			// We don't, so we need to run WebFinger
			var (acct, resolvedUri) = await WebFingerAsync(query);

			// Check the database again with the new data
			if (resolvedUri != query) user = await userSvc.GetUserFromQueryAsync(resolvedUri);
			if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
			if (user != null) return user;

			using (await KeyedLocker.LockAsync(resolvedUri))
			{
				// Pass the job on to userSvc, which will create the user
				return await userSvc.CreateUserAsync(resolvedUri, acct);
			}
		}
		catch
		{
			return null;
		}
	}

	public async Task<User> GetUpdatedUser(User user)
	{
		if (!user.NeedsUpdate) return user;

		// Prevent multiple background tasks from being started
		user.LastFetchedAt = DateTime.UtcNow;
		await db.Users.Where(p => p == user)
		        .ExecuteUpdateAsync(p => p.SetProperty(i => i.LastFetchedAt, _ => user.LastFetchedAt));

		var success = false;

		try
		{
			var task = followupTaskSvc.ExecuteTask("UpdateUserAsync", async provider =>
			{
				// Get a fresh UserService instance in a new scope
				var bgUserSvc = provider.GetRequiredService<UserService>();

				// Use the id overload so it doesn't attempt to insert in the main thread's DbContext
				await bgUserSvc.UpdateUserAsync(user.Id);
				success = true;
			});

			// Return early, but continue execution in background
			await task.WaitAsync(TimeSpan.FromMilliseconds(1500));
		}
		catch (Exception e)
		{
			if (e is TimeoutException)
				logger.LogDebug("UpdateUserAsync timed out for user {user}", user.Uri);
			else if (e is not AuthFetchException { Message: "The remote user no longer exists." })
				logger.LogError("UpdateUserAsync for user {user} failed with {error}", user.Uri, e.Message);
		}

		if (success)
			await db.ReloadEntityRecursivelyAsync(user);

		return user;
	}
}