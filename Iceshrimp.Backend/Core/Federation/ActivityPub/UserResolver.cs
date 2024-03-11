using AsyncKeyedLock;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserResolver(
	ILogger<UserResolver> logger,
	UserService userSvc,
	WebFingerService webFingerSvc,
	FollowupTaskService followupTaskSvc,
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
	 * 2. WebFinger(ap_uri), find the first acct: URI in [subject] + aliases, that's candidate_acct_uri
	 * 3. WebFinger(candidate_acct_uri), validate it also points to ap_uri. If so, you have acct_uri
	 * 4. Failing this, acct_uri = "acct:" + preferredUsername from AP actor + "@" + hostname from ap_uri (TODO: implement this)
	 *
	 * Avoid repeating WebFinger's with same URI for performance, TODO: optimize away validation checks when the domain matches
	 */

	//TODO: split domain handling can get stuck in an infinite loop, limit recursion
	private async Task<(string Acct, string Uri)> WebFingerAsync(string query)
	{
		logger.LogDebug("Running WebFinger for query '{query}'", query);

		var responses = new Dictionary<string, WebFingerResponse>();
		var fingerRes = await webFingerSvc.ResolveAsync(query);
		if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{query}'");
		responses.Add(query, fingerRes);

		var apUri = fingerRes.Links.FirstOrDefault(p => p.Rel == "self" && p.Type == "application/activity+json")
		                     ?.Href;
		if (apUri == null)
			throw new GracefulException($"WebFinger response for '{query}' didn't contain a candidate link");

		fingerRes = responses.GetValueOrDefault(apUri);
		if (fingerRes == null)
		{
			logger.LogDebug("AP uri didn't match query, re-running WebFinger for '{apUri}'", apUri);

			fingerRes = await webFingerSvc.ResolveAsync(apUri);

			if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{apUri}'");
			responses.Add(apUri, fingerRes);
		}

		var acctUri = (fingerRes.Aliases ?? []).Prepend(fingerRes.Subject).FirstOrDefault(p => p.StartsWith("acct:"));
		if (acctUri == null)
			throw new GracefulException($"WebFinger response for '{apUri}' didn't contain any acct uris");

		fingerRes = responses.GetValueOrDefault(acctUri);
		if (fingerRes == null)
		{
			logger.LogDebug("Acct uri didn't match query, re-running WebFinger for '{acctUri}'", acctUri);

			fingerRes = await webFingerSvc.ResolveAsync(acctUri);

			if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{acctUri}'");
			responses.Add(acctUri, fingerRes);
		}

		var finalAcct = fingerRes.Subject;
		var finalUri = fingerRes.Links.FirstOrDefault(p => p is { Rel: "self", Type: "application/activity+json" })
		                        ?.Href ??
		               throw new GracefulException("Final AP URI was null");

		return (finalAcct, finalUri);
	}

	private static string NormalizeQuery(string query)
	{
		if (query.StartsWith("https://") || query.StartsWith("http://"))
			if (query.Contains('#'))
				query = query.Split("#")[0];
			else
				return query;
		else if (query.StartsWith('@'))
			query = $"acct:{query[1..]}";
		else
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
		var user = await userSvc.GetUserFromQueryAsync(query);
		if (user != null)
			return await GetUpdatedUser(user);
		return user;
	}

	public async Task<User> ResolveAsync(string query)
	{
		query = NormalizeQuery(query);
		
		// Before we begin, let's skip local note urls
		if (query.StartsWith($"https://{config.Value.WebDomain}/notes/"))
			throw GracefulException.BadRequest("Refusing to resolve local note URL as user");

		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQueryAsync(query);
		if (user != null)
			return await GetUpdatedUser(user);

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFingerAsync(query);

		// Check the database again with the new data
		if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
		if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
		if (user != null)
			return await GetUpdatedUser(user);

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
			if (user != null)
				return await GetUpdatedUser(user);

			if (host == null) return null;

			// We don't, so we need to run WebFinger
			var (acct, uri) = await WebFingerAsync(query);

			// Check the database again with the new data
			if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
			if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
			if (user != null)
				return await GetUpdatedUser(user);

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

	public async Task<User?> ResolveAsyncOrNull(string uri)
	{
		try
		{
			// First, let's see if we already know the user
			var user = await userSvc.GetUserFromQueryAsync(uri);
			if (user != null)
				return await GetUpdatedUser(user);

			if (uri.StartsWith($"https://{config.Value.WebDomain}/")) return null;

			// We don't, so we need to run WebFinger
			var (acct, resolvedUri) = await WebFingerAsync(uri);

			// Check the database again with the new data
			if (resolvedUri != uri) user = await userSvc.GetUserFromQueryAsync(resolvedUri);
			if (user == null && acct != uri) await userSvc.GetUserFromQueryAsync(acct);
			if (user != null)
				return await GetUpdatedUser(user);

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
		user.LastFetchedAt = DateTime.UtcNow; // Prevent multiple background tasks from being started

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
			else
				logger.LogError("UpdateUserAsync for user {user} failed with {error}", user.Uri, e.Message);
		}

		if (success)
			await db.ReloadEntityRecursivelyAsync(user);

		return user;
	}
}