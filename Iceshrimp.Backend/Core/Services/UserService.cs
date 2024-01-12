using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class UserService(ILogger<UserService> logger, DatabaseContext db) {
	private static (string Username, string Host) AcctToTuple(string acct) {
		if (!acct.StartsWith("acct:")) throw new Exception("Invalid query");
		throw new NotImplementedException(); //FIXME
	}
	
	public Task<User?> GetUserFromQuery(string query) {
		if (query.StartsWith("http://") || query.StartsWith("https://")) {
			return db.Users.FirstOrDefaultAsync(p => p.Uri == query);
		}

		var tuple = AcctToTuple(query);
		return db.Users.FirstOrDefaultAsync(p => p.Username == tuple.Username && p.Host == tuple.Host);
	}
	
	public async Task<User> CreateUser(string uri, string acct) {
		throw new NotImplementedException(); //FIXME
	}
}