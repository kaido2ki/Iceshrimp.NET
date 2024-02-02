using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Extensions;

public static class EnumerableExtensions {
	public static async Task<IEnumerable<T>> AwaitAllAsync<T>(this IEnumerable<Task<T>> tasks) {
		return await Task.WhenAll(tasks);
	}

	public static async Task<IEnumerable<Status>> RenderAllForMastodonAsync(
		this IEnumerable<Note> notes, NoteRenderer renderer) {
		return await notes.Select(async p => await renderer.RenderAsync(p)).AwaitAllAsync();
	}
}