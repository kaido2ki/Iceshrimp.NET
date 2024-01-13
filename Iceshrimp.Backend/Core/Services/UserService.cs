using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(ILogger<UserService> logger, DatabaseContext db, HttpClient client, ActivityPubService apSvc) {
	private static (string Username, string Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new Exception("Invalid query");

		var split = acct[5..].Split('@');
		if (split.Length != 2) throw new Exception("Invalid query");

		return (split[0], split[1]);
	}

	public Task<User?> GetUserFromQuery(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://")) {
			return db.Users.FirstOrDefaultAsync(p => p.Uri == query);
		}

		var tuple = AcctToTuple(query);
		return db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}

	public async Task<User> CreateUser(string uri, string acct) {
		logger.LogInformation("Creating user {acct} with uri {uri}", acct, uri);
		var actor = await apSvc.FetchActor(uri);
		logger.LogInformation("Got actor: {inbox}", actor.Inbox);
		
		throw new NotImplementedException(); //FIXME
	}
}