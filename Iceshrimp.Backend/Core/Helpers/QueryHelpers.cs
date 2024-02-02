using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Helpers;

public static class QueryHelpers {
	public static IQueryable<Note> WithIncludes(this IQueryable<Note> query) {
		return query.Include(p => p.User)
		            .Include(p => p.Renote)
		            .ThenInclude(p => p != null ? p.User : null)
		            .Include(p => p.Reply)
		            .ThenInclude(p => p != null ? p.User : null);
	}

	public static IQueryable<Note> HasVisibility(this IQueryable<Note> query, Note.NoteVisibility visibility) {
		return query.Where(note => note.Visibility == visibility);
	}

	public static IQueryable<Note> IsFollowedBy(this IQueryable<Note> query, User user) {
		return query.Where(note => note.User.FollowingFollowees.Any(following => following.Follower == user));
	}

	public static IQueryable<Note> OrderByIdDesc(this IQueryable<Note> query) {
		return query.OrderByDescending(note => note.Id);
	}
}