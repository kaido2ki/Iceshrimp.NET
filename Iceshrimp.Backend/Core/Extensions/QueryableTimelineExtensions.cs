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

	private const string Prefix = "following-query-heuristic";

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

	public static IQueryable<User> NeedsTimelineHeuristicUpdate(
		this IQueryable<User> query, DatabaseContext db, TimeSpan maxRemainingTtl
	)
	{
		var cutoff = DateTime.UtcNow + maxRemainingTtl;
		return query.Where(u => !db.CacheStore.Any(c => c.Key == Prefix + ':' + u.Id && c.Expiry > cutoff));
	}

	public static async Task ResetHeuristicAsync(User user, CacheService cache)
	{
		await cache.ClearAsync($"{Prefix}:{user.Id}");
	}

	public static async Task<int> GetHeuristicAsync(
		User user, DatabaseContext db, CacheService cache, bool forceUpdate = false
	)
	{
		return await cache.FetchValueAsync($"{Prefix}:{user.Id}", TimeSpan.FromHours(24), FetchHeuristicAsync,
		                                   forceUpdate: forceUpdate);

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall")]
		async Task<int> FetchHeuristicAsync()
		{
			var latestNote = await db.Notes.OrderByDescending(p => p.Id)
			                         .Select(p => new { p.CreatedAt })
			                         .FirstOrDefaultAsync()
			                 ?? new { CreatedAt = DateTime.UtcNow };

			//TODO: maybe we should express this as a ratio between matching and non-matching posts
			return await db.Notes
			               .Where(p => p.CreatedAt > latestNote.CreatedAt - TimeSpan.FromDays(7))
			               .FollowingAndOwnLowFreq(user, db)
			               .OrderByDescending(p => p.Id)
			               .Take(Cutoff + 1)
			               .CountAsync();
		}
	}
}
