using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Federation.WebFinger;

public class UserResolver(ILogger<UserResolver> logger) {
	private static string AcctToDomain(string acct) =>
		acct.StartsWith("acct:") && acct.Contains('@')
			? acct[5..].Split('@')[1]
			: throw new Exception("Invalid acct");

	/*
	 * Split domain logic:
	 * 1. Get WebFinger response for query
	 * 2. [...]
	 * TODO: finish description and implement the rest
	 */

	public async Task<User> Resolve(string query) {
		var finger    = new WebFinger(query);
		var fingerRes = await finger.Resolve();
		if (fingerRes == null) throw new Exception($"Failed to WebFinger '{query}'");
		if (finger.Domain != AcctToDomain(fingerRes.Subject)) {
			logger.LogInformation("possible split domain deployment detected, repeating webfinger");

			finger    = new WebFinger(fingerRes.Subject);
			fingerRes = await finger.Resolve();
		}

		throw new NotImplementedException("stub method return");
	}
}