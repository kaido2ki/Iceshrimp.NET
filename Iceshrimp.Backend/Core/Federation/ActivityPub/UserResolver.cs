using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Federation.ActivityPub;

public class UserResolver(ILogger<UserResolver> logger, UserService userSvc, WebFingerService webFingerSvc) {
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
	private async Task<(string Acct, string Uri)> WebFingerAsync(string query) {
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
		if (fingerRes == null) {
			logger.LogDebug("AP uri didn't match query, re-running WebFinger for '{apUri}'", apUri);

			fingerRes = await webFingerSvc.ResolveAsync(apUri);

			if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{apUri}'");
			responses.Add(apUri, fingerRes);
		}

		var acctUri = (fingerRes.Aliases ?? []).Prepend(fingerRes.Subject).FirstOrDefault(p => p.StartsWith("acct:"));
		if (acctUri == null)
			throw new GracefulException($"WebFinger response for '{apUri}' didn't contain any acct uris");

		fingerRes = responses.GetValueOrDefault(acctUri);
		if (fingerRes == null) {
			logger.LogDebug("Acct uri didn't match query, re-running WebFinger for '{acctUri}'", acctUri);

			fingerRes = await webFingerSvc.ResolveAsync(acctUri);

			if (fingerRes == null) throw new GracefulException($"Failed to WebFinger '{acctUri}'");
			responses.Add(acctUri, fingerRes);
		}

		var finalAcct = fingerRes.Subject;
		var finalUri = fingerRes.Links.FirstOrDefault(p => p.Rel == "self" && p.Type == "application/activity+json")
		                        ?.Href ?? throw new GracefulException("Final AP URI was null");

		return (finalAcct, finalUri);
	}

	private static string NormalizeQuery(string query) {
		if ((query.StartsWith("https://") || query.StartsWith("http://")) && query.Contains('#'))
			query = query.Split("#")[0];
		if (query.StartsWith('@'))
			query = $"acct:{query[1..]}";

		return query;
	}

	public async Task<User> ResolveAsync(string query) {
		query = NormalizeQuery(query);

		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQueryAsync(query);
		if (user != null) return user;

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFingerAsync(query);

		// Check the database again with the new data
		if (uri != query) user = await userSvc.GetUserFromQueryAsync(uri);
		if (user == null && acct != query) await userSvc.GetUserFromQueryAsync(acct);
		if (user != null) return user;

		// Pass the job on to userSvc, which will create the user
		return await userSvc.CreateUserAsync(uri, acct);
	}
}