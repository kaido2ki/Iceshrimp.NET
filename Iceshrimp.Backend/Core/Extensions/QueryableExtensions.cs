using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class NoteQueryableExtensions {
	public static IQueryable<Note> IncludeCommonProperties(this IQueryable<Note> query) {
		return query.Include(p => p.User)
		            .ThenInclude(p => p.UserProfile)
		            .Include(p => p.Renote)
		            .ThenInclude(p => p != null ? p.User : null)
		            .Include(p => p.Reply)
		            .ThenInclude(p => p != null ? p.User : null);
	}

	public static IQueryable<User> IncludeCommonProperties(this IQueryable<User> query) {
		return query.Include(p => p.UserProfile);
	}

	public static IQueryable<Note> Paginate(this IQueryable<Note> query, PaginationQuery p, int defaultLimit,
	                                        int maxLimit) {
		if (p is { SinceId: not null, MinId: not null })
			throw GracefulException.BadRequest("Can't use sinceId and minId params simultaneously");

		query = p switch {
			{ SinceId: not null, MaxId: not null } => query
			                                          .Where(note => note.Id.IsGreaterThan(p.SinceId) &&
			                                                         note.Id.IsLessThan(p.MaxId))
			                                          .OrderByDescending(note => note.Id),
			{ MinId: not null, MaxId: not null } => query
			                                        .Where(note => note.Id.IsGreaterThan(p.MinId) &&
			                                                       note.Id.IsLessThan(p.MaxId))
			                                        .OrderBy(note => note.Id),
			{ SinceId: not null } => query.Where(note => note.Id.IsGreaterThan(p.SinceId))
			                              .OrderByDescending(note => note.Id),
			{ MinId: not null } => query.Where(note => note.Id.IsGreaterThan(p.MinId)).OrderBy(note => note.Id),
			{ MaxId: not null } => query.Where(note => note.Id.IsLessThan(p.MaxId)).OrderByDescending(note => note.Id),
			_                   => query.OrderByDescending(note => note.Id)
		};

		return query.Take(Math.Min(p.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<Note> HasVisibility(this IQueryable<Note> query, Note.NoteVisibility visibility) {
		return query.Where(note => note.Visibility == visibility);
	}

	public static IQueryable<Note> FilterByFollowingAndOwn(this IQueryable<Note> query, User user) {
		return query.Where(note => note.User == user || note.User.IsFollowedBy(user));
	}

	public static IQueryable<Note> EnsureVisibleFor(this IQueryable<Note> query, User? user) {
		if (user == null)
			return query.Where(note => note.VisibilityIsPublicOrHome)
			            .Where(note => !note.LocalOnly);

		return query.Where(note => note.IsVisibleFor(user));
	}

	public static IQueryable<Note> FilterBlocked(this IQueryable<Note> query, User user) {
		return query.Where(note => !note.User.IsBlocking(user) && !note.User.IsBlockedBy(user))
		            .Where(note => note.Renote == null ||
		                           (!note.Renote.User.IsBlockedBy(user) && !note.Renote.User.IsBlocking(user)))
		            .Where(note => note.Reply == null ||
		                           (!note.Reply.User.IsBlockedBy(user) && !note.Reply.User.IsBlocking(user)));
	}

	public static IQueryable<Note> FilterMuted(this IQueryable<Note> query, User user) {
		//TODO: handle muted instances

		return query.Where(note => !note.User.IsMuting(user))
		            .Where(note => note.Renote == null || !note.Renote.User.IsMuting(user))
		            .Where(note => note.Reply == null || !note.Reply.User.IsMuting(user));
	}

	public static IQueryable<Note> FilterHiddenListMembers(this IQueryable<Note> query, User user) {
		return query.Where(note => note.User.UserListMembers.Any(p => p.UserList.User == user &&
		                                                              p.UserList.HideFromHomeTl));
	}

	public static async Task<IEnumerable<Status>> RenderAllForMastodonAsync(
		this IQueryable<Note> notes, NoteRenderer renderer) {
		var list = await notes.ToListAsync();
		return await renderer.RenderManyAsync(list);
	}
}