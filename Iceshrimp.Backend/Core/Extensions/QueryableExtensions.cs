using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class NoteQueryableExtensions {
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

	public static IQueryable<Note> FilterByFollowingAndOwn(this IQueryable<Note> query, User user) {
		return query.Where(note => note.User == user || note.User.IsFollowedBy(user));
	}

	public static IQueryable<Note> OrderByIdDesc(this IQueryable<Note> query) {
		return query.OrderByDescending(note => note.Id);
	}

	public static async Task<IEnumerable<Status>> RenderAllForMastodonAsync(
		this IQueryable<Note> notes, NoteRenderer renderer) {
		var list = await notes.ToListAsync();
		return await list.Select(renderer.RenderAsync).AwaitAllAsync();
	}
}