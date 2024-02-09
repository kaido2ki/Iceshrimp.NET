using Iceshrimp.Backend.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class DatabaseMaintenanceService(DatabaseContext db) {
	public async Task RecomputeNoteCountersAsync() {
		await db.Notes.ExecuteUpdateAsync(p => p.SetProperty(n => n.RenoteCount,
		                                                     n => db.Notes.Count(r => r.IsPureRenote))
		                                        .SetProperty(n => n.RepliesCount,
		                                                     n => db.Notes.Count(r => r.Reply == n)));
		//TODO: update reaction counts as well? (can likely not be done database-side :/)
	}
	
	public async Task RecomputeUserCountersAsync() {
		await db.Users.ExecuteUpdateAsync(p => p.SetProperty(u => u.FollowersCount,
		                                                     u => db.Followings.Count(f => f.Followee == u))
		                                        .SetProperty(u => u.FollowingCount,
		                                                     u => db.Followings.Count(f => f.Follower == u))
		                                        .SetProperty(u => u.NotesCount,
		                                                     u => db.Notes.Count(n => n.User == u && !n.IsPureRenote)));
	}
}