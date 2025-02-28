using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Shared.Attributes;
using Iceshrimp.Backend.Controllers.Shared.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class QueryableExtensions
{
	/// <summary>
	/// This helper method allows consumers to obtain the performance &amp; memory footprint benefits of chunked DB transactions,
	/// while not requiring them to work with chunks instead of a regular enumerator.
	/// </summary>
	/// <remarks>
	/// Make sure to call .OrderBy() on the query, otherwise the results will be unpredictable.
	/// Furthermore, this method is unsuitable for cases where the consumer removes elements from the original collection.
	/// </remarks>
	/// <returns>
	/// The result set as an IAsyncEnumerable. Makes one DB roundtrip at the start of each chunk.
	/// Successive items in the chunk are yielded instantaneously.
	/// </returns>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static async IAsyncEnumerable<T> AsChunkedAsyncEnumerable<T>(this IQueryable<T> query, int chunkSize)
	{
		var offset = 0;
		while (true)
		{
			var res = await query.Skip(offset).Take(chunkSize).ToArrayAsync();
			if (res.Length == 0) break;
			foreach (var item in res) yield return item;
			if (res.Length < chunkSize) break;
			offset += chunkSize;
		}
	}

	/// <inheritdoc cref="AsChunkedAsyncEnumerable{T}(System.Linq.IQueryable{T},int)" select="summary|returns"/>
	/// <remarks>
	/// This overload requires you to pass a predicate to the identifier.
	/// .OrderBy(<paramref name="idPredicate"/>) is appended to the query.
	/// Set the <paramref name="hook"/> parameter to append things to the query after pagination, for cases where query translation would fail otherwise.
	/// </remarks>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static async IAsyncEnumerable<TResult> AsChunkedAsyncEnumerable<TResult>(
		this IQueryable<TResult> query, int chunkSize, Expression<Func<TResult, string>> idPredicate,
		Func<IQueryable<TResult>, IQueryable<TResult>>? hook = null
	)
	{
		var pred = idPredicate.Compile();
		query = query.OrderBy(idPredicate);

		string? last = null;
		while (true)
		{
			// ReSharper disable once AccessToModifiedClosure
			var final = last is not null ? query.Where(idPredicate.Compose(p => p.IsGreaterThan(last))) : query;
			if (hook != null)
				final = hook(final);
			var res = await final.Take(chunkSize).ToArrayAsync();
			if (res.Length == 0) break;
			foreach (var item in res) yield return item;
			if (res.Length < chunkSize) break;
			last = pred.Invoke(res.Last());
		}
	}

	/// <inheritdoc cref="AsChunkedAsyncEnumerable{TResult}(System.Linq.IQueryable{TResult},int,System.Linq.Expressions.Expression{System.Func{TResult,string}},System.Func{System.Linq.IQueryable{TResult},System.Linq.IQueryable{TResult}}?)"/>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static async IAsyncEnumerable<TResult> AsChunkedAsyncEnumerable<TResult>(
		this IQueryable<TResult> query, int chunkSize, Expression<Func<TResult, Guid>> idPredicate,
		Func<IQueryable<TResult>, IQueryable<TResult>>? hook = null
	)
	{
		var pred = idPredicate.Compile();
		query = query.OrderBy(idPredicate);

		Guid? last = null;
		while (true)
		{
			// ReSharper disable once AccessToModifiedClosure
			var final = last is not null ? query.Where(idPredicate.Compose(p => p > last)) : query;
			if (hook != null)
				final = hook(final);
			var res = await final.Take(chunkSize).ToArrayAsync();
			if (res.Length == 0) break;
			foreach (var item in res) yield return item;
			if (res.Length < chunkSize) break;
			last = pred.Invoke(res.Last());
		}
	}

	/// <inheritdoc cref="AsChunkedAsyncEnumerable{TResult}(System.Linq.IQueryable{TResult},int,System.Linq.Expressions.Expression{System.Func{TResult,string}},System.Func{System.Linq.IQueryable{TResult},System.Linq.IQueryable{TResult}}?)"/>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static async IAsyncEnumerable<TResult> AsChunkedAsyncEnumerable<TResult>(
		this IQueryable<TResult> query, int chunkSize, Expression<Func<TResult, int>> idPredicate,
		Func<IQueryable<TResult>, IQueryable<TResult>>? hook = null
	)
	{
		var pred = idPredicate.Compile();
		query = query.OrderBy(idPredicate);

		int? last = null;
		while (true)
		{
			// ReSharper disable once AccessToModifiedClosure
			var final = last is not null ? query.Where(idPredicate.Compose(p => p > last)) : query;
			if (hook != null)
				final = hook(final);
			var res = await final.Take(chunkSize).ToArrayAsync();
			if (res.Length == 0) break;
			foreach (var item in res) yield return item;
			if (res.Length < chunkSize) break;
			last = pred.Invoke(res.Last());
		}
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		MastodonPaginationQuery pq,
		int defaultLimit,
		int maxLimit
	) where T : IEntity
	{
		if (pq.Limit is < 1)
			throw GracefulException.BadRequest("Limit cannot be less than 1");

		if (pq is { SinceId: not null, MinId: not null })
			throw GracefulException.BadRequest("Can't use sinceId and minId params simultaneously");

		// @formatter:off
		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query.Where(p => p.Id.IsGreaterThan(pq.SinceId) && p.Id.IsLessThan(pq.MaxId))
			                                               .OrderByDescending(p => p.Id),
			{ MinId: not null, MaxId: not null }   => query.Where(p => p.Id.IsGreaterThan(pq.MinId) && p.Id.IsLessThan(pq.MaxId))
			                                               .OrderBy(p => p.Id),
			{ SinceId: not null }                  => query.Where(p => p.Id.IsGreaterThan(pq.SinceId))
			                                               .OrderByDescending(p => p.Id),
			{ MinId: not null }                    => query.Where(p => p.Id.IsGreaterThan(pq.MinId))
			                                               .OrderBy(p => p.Id),
			{ MaxId: not null }                    => query.Where(p => p.Id.IsLessThan(pq.MaxId))
			                                               .OrderByDescending(p => p.Id),
			_                                      => query.OrderByDescending(p => p.Id)
		};
		// @formatter:on

		return query.Skip(pq.Offset ?? 0).Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, string>> predicate,
		MastodonPaginationQuery pq,
		int defaultLimit,
		int maxLimit
	) where T : IEntity
	{
		if (pq.Limit is < 1)
			throw GracefulException.BadRequest("Limit cannot be less than 1");

		if (pq is { SinceId: not null, MinId: not null })
			throw GracefulException.BadRequest("Can't use sinceId and minId params simultaneously");

		// @formatter:off
		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.SinceId) && id.IsLessThan(pq.MaxId)))
			                                               .OrderByDescending(predicate),
			{ MinId: not null, MaxId: not null }   => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.MinId) && id.IsLessThan(pq.MaxId)))
			                                               .OrderBy(predicate),
			{ SinceId: not null }                  => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.SinceId)))
			                                               .OrderByDescending(predicate),
			{ MinId: not null }                    => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.MinId)))
			                                               .OrderBy(predicate),
			{ MaxId: not null }                    => query.Where(predicate.Compose(id => id.IsLessThan(pq.MaxId)))
			                                               .OrderByDescending(predicate),
			_                                      => query.OrderByDescending(predicate)
		};
		// @formatter:on

		return query.Skip(pq.Offset ?? 0).Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, long>> predicate,
		MastodonPaginationQuery pq,
		int defaultLimit,
		int maxLimit
	) where T : IEntity
	{
		if (pq.Limit is < 1)
			throw GracefulException.BadRequest("Limit cannot be less than 1");

		if (pq is { SinceId: not null, MinId: not null })
			throw GracefulException.BadRequest("Can't use sinceId and minId params simultaneously");

		long? sinceId = null;
		long? minId   = null;
		long? maxId   = null;

		if (pq.SinceId != null)
		{
			if (!long.TryParse(pq.SinceId, out var res))
				throw GracefulException.BadRequest("sinceId must be an integer");
			sinceId = res;
		}

		if (pq.MinId != null)
		{
			if (!long.TryParse(pq.MinId, out var res))
				throw GracefulException.BadRequest("minId must be an integer");
			minId = res;
		}

		if (pq.MaxId != null)
		{
			if (!long.TryParse(pq.MaxId, out var res))
				throw GracefulException.BadRequest("maxId must be an integer");
			maxId = res;
		}

		// @formatter:off
		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query.Where(predicate.Compose(id => id > sinceId && id < maxId))
			                                               .OrderByDescending(predicate),
			{ MinId: not null, MaxId: not null }   => query.Where(predicate.Compose(id => id > minId && id < maxId))
			                                               .OrderBy(predicate),
			{ SinceId: not null }                  => query.Where(predicate.Compose(id => id > sinceId))
			                                               .OrderByDescending(predicate),
			{ MinId: not null }                    => query.Where(predicate.Compose(id => id > minId))
			                                               .OrderBy(predicate),
			{ MaxId: not null }                    => query.Where(predicate.Compose(id => id < maxId))
			                                               .OrderByDescending(predicate),
			_                                      => query.OrderByDescending(predicate)
		};
		// @formatter:on

		return query.Skip(pq.Offset ?? 0).Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		PaginationQuery pq,
		int defaultLimit,
		int maxLimit
	) where T : IEntity
	{
		if (pq.Limit is < 1)
			throw GracefulException.BadRequest("Limit cannot be less than 1");

		// @formatter:off
		query = pq switch
		{
			{ MinId: not null, MaxId: not null } => query.Where(p => p.Id.IsGreaterThan(pq.MinId) && p.Id.IsLessThan(pq.MaxId))
			                                             .OrderBy(p => p.Id),
			{ MinId: not null }                  => query.Where(p => p.Id.IsGreaterThan(pq.MinId))
			                                             .OrderBy(p => p.Id),
			{ MaxId: not null }                  => query.Where(p => p.Id.IsLessThan(pq.MaxId))
			                                             .OrderByDescending(p => p.Id),
			_                                    => query.OrderByDescending(p => p.Id)
		};
		// @formatter:on

		return query.Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var attr = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPaginationAttribute>();
		if (attr == null)
			throw new Exception("Route doesn't have a IPaginationAttribute");

		return Paginate(query, pq, attr.DefaultLimit, attr.MaxLimit);
	}

	public static IQueryable<T> PaginateByOffset<T>(
		this IQueryable<T> query,
		MastodonPaginationQuery pq,
		int defaultLimit,
		int maxLimit
	) where T : IEntity
	{
		if (pq.Limit is < 1)
			throw GracefulException.BadRequest("Limit cannot be less than 1");

		return query.Skip(pq.Offset ?? 0).Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> PaginateByOffset<T>(
		this IQueryable<T> query,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var attr = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPaginationAttribute>();
		if (attr == null)
			throw new Exception("Route doesn't have a IPaginationAttribute");

		return PaginateByOffset(query, pq, attr.DefaultLimit, attr.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, string>> predicate,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var attr = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPaginationAttribute>();
		if (attr == null)
			throw new Exception("Route doesn't have a IPaginationAttribute");

		return Paginate(query, predicate, pq, attr.DefaultLimit, attr.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, long>> predicate,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var attr = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPaginationAttribute>();
		if (attr == null)
			throw new Exception("Route doesn't have a IPaginationAttribute");

		return Paginate(query, predicate, pq, attr.DefaultLimit, attr.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		PaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var attr = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IPaginationAttribute>();
		if (attr == null)
			throw new Exception("Route doesn't have a IPaginationAttribute");

		return Paginate(query, pq, attr.DefaultLimit, attr.MaxLimit);
	}

	public static IQueryable<EntityWrapper<TResult>> Wrap<TSource, TResult>(
		this IQueryable<TSource> query, Expression<Func<TSource, TResult>> predicate
	) where TSource : IEntity
	{
		return query.Select(p => new EntityWrapper<TResult> { Id = p.Id, Entity = predicate.Compile().Invoke(p) });
	}

	public static IQueryable<Note> HasVisibility(this IQueryable<Note> query, Note.NoteVisibility visibility)
	{
		return query.Where(note => note.Visibility == visibility);
	}

	public static IQueryable<Note> FilterByUser(this IQueryable<Note> query, User user)
	{
		return query.Where(note => note.User == user);
	}

	public static IQueryable<Note> FilterByUser(this IQueryable<Note> query, string? userId)
	{
		return userId != null ? query.Where(note => note.UserId == userId) : query;
	}

	public static IQueryable<Note> EnsureVisibleFor(this IQueryable<Note> query, User? user)
	{
		return user == null
			? query.Where(note => note.VisibilityIsPublicOrHome && !note.LocalOnly)
			: query.Where(note => note.IsVisibleFor(user));
	}

	public static IQueryable<TSource> EnsureNoteVisibilityFor<TSource>(
		this IQueryable<TSource> query, Expression<Func<TSource, Note?>> predicate, User? user
	)
	{
		return query.Where(user == null
			                   ? predicate.Compose(p => p == null || (p.VisibilityIsPublicOrHome && !p.LocalOnly))
			                   : predicate.Compose(p => p == null || p.IsVisibleFor(user)));
	}

	public static IQueryable<Note> PrecomputeVisibilities(this IQueryable<Note> query, User? user)
	{
		return query.Select(p => p.WithPrecomputedVisibilities(p.Reply != null && p.Reply.IsVisibleFor(user),
		                                                       p.Renote != null && p.Renote.IsVisibleFor(user),
		                                                       p.Renote != null &&
		                                                       p.Renote.Renote != null &&
		                                                       p.Renote.Renote.IsVisibleFor(user)));
	}

	public static IQueryable<Note> PrecomputeNoteContextVisibilities(this IQueryable<Note> query, User? user)
	{
		return query.Select(p => p.WithPrecomputedVisibilities(p.Reply != null && p.Reply.IsVisibleFor(user),
		                                                       p.Renote != null && p.Renote.IsVisibleFor(user),
		                                                       p.Renote != null &&
		                                                       false));
	}

	public static IQueryable<Notification> PrecomputeNoteVisibilities(this IQueryable<Notification> query, User user)
	{
		return query.Select(p => p.WithPrecomputedNoteVisibilities(p.Note != null &&
		                                                           p.Note.Reply != null &&
		                                                           p.Note.Reply.IsVisibleFor(user),
		                                                           p.Note != null &&
		                                                           p.Note.Renote != null &&
		                                                           p.Note.Renote.IsVisibleFor(user),
		                                                           p.Note != null &&
		                                                           p.Note.Renote != null &&
		                                                           p.Note.Renote.Renote != null &&
		                                                           p.Note.Renote.Renote.IsVisibleFor(user)));
	}

	public static IQueryable<User> PrecomputeRelationshipData(this IQueryable<User> query, User user)
	{
		return query.Select(p => p.WithPrecomputedBlockStatus(p.IsBlocking(user), p.IsBlockedBy(user))
		                          .WithPrecomputedMuteStatus(p.IsMuting(user), p.IsMutedBy(user))
		                          .WithPrecomputedFollowStatus(p.IsFollowing(user), p.IsFollowedBy(user),
		                                                       p.IsRequested(user), p.IsRequestedBy(user)));
	}

	public static IQueryable<Notification> FilterHiddenNotifications(
		this IQueryable<Notification> query, User user, DatabaseContext db
	)
	{
		var blocks = db.Blockings.Where(i => i.Blocker == user).Select(p => p.BlockeeId);
		var mutes  = db.Mutings.Where(i => i.Muter == user).Select(p => p.MuteeId);
		var hidden = blocks.Concat(mutes);

		return query.Where(p => !hidden.Contains(p.NotifierId) && (p.Note == null || !hidden.Contains(p.Note.Id)));
	}

	public static IQueryable<Note> FilterHiddenConversations(this IQueryable<Note> query, User user, DatabaseContext db)
	{
		//TODO: handle muted instances

		var blocks = db.Blockings.Where(i => i.Blocker == user).Select(p => p.BlockeeId);
		var mutes  = db.Mutings.Where(i => i.Muter == user).Select(p => p.MuteeId);
		var hidden = blocks.Concat(mutes);

		return query.Where(p => p.VisibleUserIds.IsDisjoint(hidden));
	}

	public static IQueryable<Note> FilterMutedThreads(this IQueryable<Note> query, User user, DatabaseContext db)
	{
		return query.Where(p => p.User == user ||
		                        !db.NoteThreadMutings.Any(m => m.User == user && m.ThreadId == p.ThreadId));
	}

	public static IQueryable<Notification> FilterMutedThreads(
		this IQueryable<Notification> query, User user, DatabaseContext db
	)
	{
		return query.Where(p => p.Note == null ||
		                        !db.NoteThreadMutings.Any(m => m.User == user && m.ThreadId == p.Note.ThreadId));
	}

	private static (IQueryable<string> hidden, IQueryable<string>? mentionsHidden) FilterHiddenInternal(
		User? user,
		DatabaseContext db,
		bool filterOutgoingBlocks = true, bool filterMutes = true,
		bool filterHiddenListMembers = false,
		string? except = null
	)
	{
		//TODO: handle muted instances

		var                 hidden         = db.Blockings.Where(p => p.Blockee == user).Select(p => p.BlockerId);
		IQueryable<string>? mentionsHidden = null;

		if (filterOutgoingBlocks)
		{
			var blockOut = db.Blockings.Where(p => p.Blocker == user).Select(p => p.BlockeeId);
			hidden         = hidden.Concat(blockOut);
			mentionsHidden = mentionsHidden == null ? blockOut : mentionsHidden.Concat(blockOut);
		}

		if (filterMutes)
		{
			var mute = db.Mutings.Where(p => p.Muter == user).Select(p => p.MuteeId);
			hidden         = hidden.Concat(mute);
			mentionsHidden = mentionsHidden == null ? mute : mentionsHidden.Concat(mute);
		}

		if (filterHiddenListMembers)
		{
			var list = db.UserListMembers.Where(p => p.UserList.User == user && p.UserList.HideFromHomeTl)
			             .Select(p => p.UserId);
			hidden         = hidden.Concat(list);
			mentionsHidden = mentionsHidden == null ? list : mentionsHidden.Concat(list);
		}

		if (except != null)
		{
			hidden         = hidden.Where(p => p != except);
			mentionsHidden = mentionsHidden?.Where(p => p != except);
		}

		return (hidden, mentionsHidden);
	}

	private static Expression<Func<Note, bool>> FilterHiddenExpr(
		IQueryable<string> hidden, IQueryable<string>? mentionsHidden, bool filterMentions
	)
	{
		if (filterMentions && mentionsHidden != null)
		{
			return note => !hidden.Contains(note.UserId) &&
			               !hidden.Contains(note.RenoteUserId) &&
			               !hidden.Contains(note.ReplyUserId) &&
			               note.Mentions.IsDisjoint(mentionsHidden) &&
			               (note.Renote == null || !hidden.Contains(note.Renote.RenoteUserId)) &&
			               (note.Renote == null || note.Renote.Mentions.IsDisjoint(mentionsHidden));
		}

		return note => !hidden.Contains(note.UserId) &&
		               !hidden.Contains(note.RenoteUserId) &&
		               !hidden.Contains(note.ReplyUserId) &&
		               (note.Renote == null || !hidden.Contains(note.Renote.RenoteUserId));
	}

	public static IQueryable<TSource> FilterHidden<TSource>(
		this IQueryable<TSource> query, Expression<Func<TSource, Note>> pred, User? user,
		DatabaseContext db,
		bool filterOutgoingBlocks = true, bool filterMutes = true,
		bool filterHiddenListMembers = false, bool filterMentions = true,
		string? except = null
	)
	{
		if (user == null)
			return query;

		var (hidden, mentionsHidden) = FilterHiddenInternal(user, db, filterOutgoingBlocks, filterMutes,
		                                                    filterHiddenListMembers, except);

		return query.Where(pred.Compose(FilterHiddenExpr(hidden, mentionsHidden, filterMentions)));
	}

	public static IQueryable<Note> FilterHidden(
		this IQueryable<Note> query, User? user, DatabaseContext db,
		bool filterOutgoingBlocks = true, bool filterMutes = true,
		bool filterHiddenListMembers = false, bool filterMentions = true,
		string? except = null
	)
	{
		if (user == null)
			return query;

		var (hidden, mentionsHidden) = FilterHiddenInternal(user, db, filterOutgoingBlocks, filterMutes,
		                                                    filterHiddenListMembers, except);

		return query.Where(FilterHiddenExpr(hidden, mentionsHidden, filterMentions));
	}

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	[SuppressMessage("ReSharper", "ParameterTypeCanBeEnumerable.Global")]
	public static bool IsDisjoint<T>(this List<T> x, IQueryable<T> y) => x.All(item => !y.Contains(item));

	public static Note EnforceRenoteReplyVisibility(this Note note)
	{
		if (!(note.PrecomputedIsReplyVisible ?? false))
			note.Reply = null;
		if (!(note.PrecomputedIsRenoteVisible ?? false))
			note.Renote = null;
		if (note.Renote?.Renote != null && !(note.Renote.PrecomputedIsRenoteVisible ?? false))
			note.Renote.Renote = null;

		return note;
	}

	public static IEnumerable<Note> EnforceRenoteReplyVisibility(this IEnumerable<Note> list)
	{
		return list.Select(EnforceRenoteReplyVisibility);
	}

	public static T EnforceRenoteReplyVisibility<T>(this T source, Expression<Func<T, Note?>> predicate)
	{
		var note = predicate.Compile().Invoke(source);
		if (note == null) return source;
		if (!(note.PrecomputedIsReplyVisible ?? false))
			note.Reply = null;
		if (!(note.PrecomputedIsRenoteVisible ?? false))
			note.Renote = null;
		if (note.Renote?.Renote != null && !(note.Renote.PrecomputedIsRenoteVisible ?? false))
			note.Renote.Renote = null;

		return source;
	}

	public static IEnumerable<T> EnforceRenoteReplyVisibility<T>(
		this IEnumerable<T> list, Expression<Func<T, Note?>> predicate
	)
	{
		return list.Select(p => EnforceRenoteReplyVisibility(p, predicate));
	}

	public static async Task<List<StatusEntity>> RenderAllForMastodonAsync(
		this IQueryable<Note> notes, NoteRenderer renderer, User? user, Filter.FilterContext? filterContext = null
	)
	{
		var list = (await notes.ToListAsync())
		           .EnforceRenoteReplyVisibility()
		           .ToList();
		return (await renderer.RenderManyAsync(list, user, filterContext)).ToList();
	}

	public static async Task<List<AccountEntity>> RenderAllForMastodonAsync(
		this IQueryable<User> users, UserRenderer renderer, User? localUser
	)
	{
		var list = await users.ToListAsync();
		return (await renderer.RenderManyAsync(list, localUser)).ToList();
	}

	public static async Task<List<NotificationEntity>> RenderAllForMastodonAsync(
		this IQueryable<Notification> notifications, NotificationRenderer renderer, User user, bool isPleroma
	)
	{
		var list = (await notifications.ToListAsync())
		           .EnforceRenoteReplyVisibility(p => p.Note)
		           .ToList();
		return (await renderer.RenderManyAsync(list, user, isPleroma)).ToList();
	}

	public static IQueryable<Note> FilterByAccountStatusesRequest(
		this IQueryable<Note> query, AccountSchemas.AccountStatusesRequest request
	)
	{
		if (request.ExcludeReplies)
			query = query.Where(p => (p.Reply == null && p.ReplyUri == null) || p.ReplyUserId == p.UserId);
		if (request.ExcludeRenotes)
			query = query.Where(p => p.Renote == null && p.RenoteUri == null);
		if (request.Tagged != null)
			query = query.Where(p => p.Tags.Contains(request.Tagged.ToLowerInvariant()));
		if (request.OnlyMedia)
			query = query.Where(p => p.FileIds.Count != 0);
		if (request.Pinned)
			query = query.Where(note => note.User.HasPinned(note));

		return query;
	}

	public static IQueryable<Notification> FilterByGetNotificationsRequest(
		this IQueryable<Notification> query,
		NotificationSchemas.GetNotificationsRequest request
	)
	{
		if (request.AccountId != null)
			query = query.Where(p => p.NotifierId == request.AccountId);
		if (request.Types != null)
			query = query.Where(p => request.Types.SelectMany(NotificationEntity.DecodeType)
			                                .Distinct()
			                                .Contains(p.Type));
		if (request.ExcludeTypes != null)
			query = query.Where(p => !request.ExcludeTypes.SelectMany(NotificationEntity.DecodeType)
			                                 .Distinct()
			                                 .Contains(p.Type));

		return query;
	}

	public static IQueryable<Note> FilterByPublicTimelineRequest(
		this IQueryable<Note> query, TimelineSchemas.PublicTimelineRequest request
	)
	{
		if (request.OnlyLocal)
			query = query.Where(p => p.UserHost == null);
		if (request.OnlyRemote)
			query = query.Where(p => p.UserHost != null);
		if (request.OnlyMedia)
			query = query.Where(p => p.FileIds.Count != 0);

		return query;
	}

	public static IQueryable<Note> FilterByHashtagTimelineRequest(
		this IQueryable<Note> query, TimelineSchemas.HashtagTimelineRequest request
	)
	{
		if (request.Any.Count > 0)
			query = query.Where(p => request.Any.Any(t => p.Tags.Contains(t)));
		if (request.All.Count > 0)
			query = query.Where(p => request.All.All(t => p.Tags.Contains(t)));
		if (request.None.Count > 0)
			query = query.Where(p => request.None.All(t => !p.Tags.Contains(t)));

		return query.FilterByPublicTimelineRequest(request);
	}

	#pragma warning disable CS8602 // Dereference of a possibly null reference.
// Justification: in the context of nullable EF navigation properties, null values are ignored and therefore irrelevant.
// Source: https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#navigating-and-including-nullable-relationships

	public static IQueryable<Note> IncludeCommonProperties(this IQueryable<Note> query)
	{
		return query.Include(p => p.User.UserProfile)
		            .Include(p => p.Renote.User.UserProfile)
		            .Include(p => p.Renote.Renote.User.UserProfile)
		            .Include(p => p.Reply.User.UserProfile);
	}

	public static IQueryable<User> IncludeCommonProperties(this IQueryable<User> query)
	{
		return query.Include(p => p.UserProfile);
	}

	public static IQueryable<Bite> IncludeCommonProperties(this IQueryable<Bite> query)
	{
		return query.Include(p => p.TargetNote)
		            .Include(p => p.TargetBite)
		            .Include(p => p.User);
	}

	public static IQueryable<FollowRequest> IncludeCommonProperties(this IQueryable<FollowRequest> query)
	{
		return query.Include(p => p.Follower.UserProfile)
		            .Include(p => p.Followee.UserProfile);
	}

	public static IQueryable<NoteLike> IncludeCommonProperties(this IQueryable<NoteLike> query)
	{
		return query.Include(p => p.Note.User.UserProfile)
		            .Include(p => p.Note.Renote.User.UserProfile)
		            .Include(p => p.Note.Renote.Renote.User.UserProfile)
		            .Include(p => p.Note.Reply.User.UserProfile);
	}

	public static IQueryable<NoteBookmark> IncludeCommonProperties(this IQueryable<NoteBookmark> query)
	{
		return query.Include(p => p.Note.User.UserProfile)
		            .Include(p => p.Note.Renote.User.UserProfile)
		            .Include(p => p.Note.Renote.Renote.User.UserProfile)
		            .Include(p => p.Note.Reply.User.UserProfile);
	}

	public static IQueryable<Notification> IncludeCommonProperties(this IQueryable<Notification> query)
	{
		return query.Include(p => p.Notifiee.UserProfile)
		            .Include(p => p.Notifier.UserProfile)
		            .Include(p => p.Note.User.UserProfile)
		            .Include(p => p.Note.Renote.User.UserProfile)
		            .Include(p => p.Note.Renote.Renote.User.UserProfile)
		            .Include(p => p.Note.Reply.User.UserProfile)
		            .Include(p => p.FollowRequest.Follower.UserProfile)
		            .Include(p => p.FollowRequest.Followee.UserProfile)
		            .Include(p => p.Bite);
	}

	#pragma warning restore CS8602 // Dereference of a possibly null reference.
}