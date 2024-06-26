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
		this IQueryable<Note> query, User user, DatabaseContext db
	)
	{
		return query.Where(note => db.Followings
		                             .Where(p => p.Follower == user)
		                             .Select(p => p.FolloweeId)
		                             .Concat(new[] { user.Id })
		                             .Contains(note.UserId));
	}

	private static IQueryable<Note> FollowingAndOwnLowFreq(this IQueryable<Note> query, User user, DatabaseContext db)
		=> query.Where(note => db.Followings
		                         .Where(p => p.Follower == user)
		                         .Select(p => p.FolloweeId)
		                         .Concat(new[] { user.Id })
		                         .Contains(note.UserId));
}