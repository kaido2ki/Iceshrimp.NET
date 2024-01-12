using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

public class UserResolver(ILogger<UserResolver> logger, UserService userSvc, DatabaseContext db) {
	private static string AcctToDomain(string acct) =>
		acct.StartsWith("acct:") && acct.Contains('@')
			? acct[5..].Split('@')[1]
			: throw new Exception("Invalid acct");

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

	private async Task<(string Acct, string Uri)> WebFinger(string query) {
		logger.LogDebug("Running WebFinger for query '{query}'", query);

		var finger    = new WebFinger(query);
		var responses = new Dictionary<string, WebFingerResponse>();
		var fingerRes = await finger.Resolve();
		if (fingerRes == null) throw new Exception($"Failed to WebFinger '{query}'");
		responses.Add(query, fingerRes);

		var apUri = fingerRes.Links.FirstOrDefault(p => p.Rel == "self" && p.Type == "application/activity+json")
		                     ?.Href;
		if (apUri == null) throw new Exception($"WebFinger response for '{query}' didn't contain a candidate link");

		fingerRes = responses.GetValueOrDefault(apUri);
		if (fingerRes == null) {
			logger.LogDebug("AP uri didn't match query, re-running WebFinger for '{apUri}'", apUri);

			finger    = new WebFinger(apUri);
			fingerRes = await finger.Resolve();

			if (fingerRes == null) throw new Exception($"Failed to WebFinger '{apUri}'");
			responses.Add(apUri, fingerRes);
		}

		var acctUri = fingerRes.Aliases.Prepend(fingerRes.Subject).FirstOrDefault(p => p.StartsWith("acct:"));
		if (acctUri == null) throw new Exception($"WebFinger response for '{apUri}' didn't contain any acct uris");

		fingerRes = responses.GetValueOrDefault(acctUri);
		if (fingerRes == null) {
			logger.LogDebug("Acct uri didn't match query, re-running WebFinger for '{acctUri}'", acctUri);

			finger    = new WebFinger(acctUri);
			fingerRes = await finger.Resolve();

			if (fingerRes == null) throw new Exception($"Failed to WebFinger '{acctUri}'");
			responses.Add(acctUri, fingerRes);
		}

		var finalAcct = fingerRes.Subject;
		var finalUri = fingerRes.Links.FirstOrDefault(p => p.Rel == "self" && p.Type == "application/activity+json")
		                        ?.Href ?? throw new Exception("Final AP URI was null");

		return (finalAcct, finalUri);
	}

	private static string NormalizeQuery(string query) => query.StartsWith("acct:") ? query[5..] : query;

	public async Task<User> Resolve(string query) {
		query = NormalizeQuery(query);
		
		// First, let's see if we already know the user
		var user = await userSvc.GetUserFromQuery(query);
		if (user != null) return user;

		// We don't, so we need to run WebFinger
		var (acct, uri) = await WebFinger(query);
		
		// Check the database again with the new data
		if (uri != query) user = await userSvc.GetUserFromQuery(uri);
		if (user == null && acct != query) await userSvc.GetUserFromQuery(acct);
		if (user != null) return user;
		
		// Pass the job on to userSvc, which will create the user
		return await userSvc.CreateUser(acct, uri);
	}
}