using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class QueryableTimelineExtensions
{
	// Determined empirically in 2023. Ask zotan for the spreadsheet if you're curious.
	private const int Cutoff = 250;

	public static IQueryable<Note> FilterByFollowingAndOwn(
		this IQueryable<Note> query, User user, DatabaseContext db, int heuristic
	)
	{
		return heuristic < Cutoff
			? query.FollowingAndOwnLowFreq(user, db)
			: query.Where(note => note.User == user || note.User.IsFollowedBy(user));
	}

	private static IQueryable<Note> FollowingAndOwnLowFreq(this IQueryable<Note> query, User user, DatabaseContext db)
		=> query.Where(note => db.Followings
		                         .Where(p => p.Follower == user)
		                         .Select(p => p.FolloweeId)
		                         .Concat(new[] { user.Id })
		                         .Contains(note.UserId));

	public static async Task<int> GetHeuristic(User user, DatabaseContext db, CacheService cache)
	{
		return await cache.FetchValueAsync($"following-query-heuristic:{user.Id}",
		                                   TimeSpan.FromHours(24), FetchHeuristic);

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall")]
		async Task<int> FetchHeuristic()
		{
			var latestNote = await db.Notes.OrderByDescending(p => p.Id)
			                         .Select(p => new { p.CreatedAt })
			                         .FirstOrDefaultAsync() ??
			                 new { CreatedAt = DateTime.UtcNow };

			//TODO: maybe we should express this as a ratio between matching and non-matching posts
			return await db.Notes
			               .Where(p => p.CreatedAt > latestNote.CreatedAt - TimeSpan.FromDays(7))
			               .FollowingAndOwnLowFreq(user, db)
			               //.Select(p => new { })
			               .Take(Cutoff + 1)
			               .CountAsync();
		}
	}
}