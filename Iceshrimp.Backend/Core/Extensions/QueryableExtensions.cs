using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Iceshrimp.Backend.Controllers.Attributes;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class QueryableExtensions
{
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

		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query
			                                          .Where(p => p.Id.IsGreaterThan(pq.SinceId) &&
			                                                      p.Id.IsLessThan(pq.MaxId))
			                                          .OrderByDescending(p => p.Id),
			{ MinId: not null, MaxId: not null } => query
			                                        .Where(p => p.Id.IsGreaterThan(pq.MinId) &&
			                                                    p.Id.IsLessThan(pq.MaxId))
			                                        .OrderBy(p => p.Id),
			{ SinceId: not null } => query.Where(p => p.Id.IsGreaterThan(pq.SinceId))
			                              .OrderByDescending(p => p.Id),
			{ MinId: not null } => query.Where(p => p.Id.IsGreaterThan(pq.MinId)).OrderBy(p => p.Id),
			{ MaxId: not null } => query.Where(p => p.Id.IsLessThan(pq.MaxId)).OrderByDescending(p => p.Id),
			_                   => query.OrderByDescending(p => p.Id)
		};

		return query.Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
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

		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query
			                                          .Where(predicate.Compose(id => id.IsGreaterThan(pq.SinceId) &&
				                                                                   id.IsLessThan(pq.MaxId)))
			                                          .OrderByDescending(predicate),
			{ MinId: not null, MaxId: not null } => query
			                                        .Where(predicate.Compose(id => id.IsGreaterThan(pq.MinId) &&
				                                                                 id.IsLessThan(pq.MaxId)))
			                                        .OrderBy(predicate),
			{ SinceId: not null } => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.SinceId)))
			                              .OrderByDescending(predicate),
			{ MinId: not null } => query.Where(predicate.Compose(id => id.IsGreaterThan(pq.MinId)))
			                            .OrderBy(predicate),
			{ MaxId: not null } => query.Where(predicate.Compose(id => id.IsLessThan(pq.MaxId)))
			                            .OrderByDescending(predicate),
			_ => query.OrderByDescending(predicate)
		};

		return query.Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
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

		query = pq switch
		{
			{ SinceId: not null, MaxId: not null } => query.Where(predicate.Compose(id => id > sinceId && id < maxId))
			                                               .OrderByDescending(predicate),
			{ MinId: not null, MaxId: not null } => query.Where(predicate.Compose(id => id > minId && id < maxId))
			                                             .OrderBy(predicate),
			{ SinceId: not null } => query.Where(predicate.Compose(id => id > sinceId)).OrderByDescending(predicate),
			{ MinId: not null }   => query.Where(predicate.Compose(id => id > minId)).OrderBy(predicate),
			{ MaxId: not null }   => query.Where(predicate.Compose(id => id < maxId)).OrderByDescending(predicate),
			_                     => query.OrderByDescending(predicate)
		};

		return query.Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
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

		query = pq switch
		{
			{ MinId: not null, MaxId: not null } => query
			                                        .Where(p => p.Id.IsGreaterThan(pq.MinId) &&
			                                                    p.Id.IsLessThan(pq.MaxId))
			                                        .OrderBy(p => p.Id),
			{ MinId: not null } => query.Where(p => p.Id.IsGreaterThan(pq.MinId)).OrderBy(p => p.Id),
			{ MaxId: not null } => query.Where(p => p.Id.IsLessThan(pq.MaxId)).OrderByDescending(p => p.Id),
			_                   => query.OrderByDescending(p => p.Id)
		};

		return query.Take(Math.Min(pq.Limit ?? defaultLimit, maxLimit));
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var filter = context.ActionDescriptor.FilterDescriptors.Select(p => p.Filter)
		                    .OfType<LinkPaginationAttribute>()
		                    .FirstOrDefault();
		if (filter == null)
			throw new GracefulException("Route doesn't have a LinkPaginationAttribute");

		return Paginate(query, pq, filter.DefaultLimit, filter.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, string>> predicate,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var filter = context.ActionDescriptor.FilterDescriptors.Select(p => p.Filter)
		                    .OfType<LinkPaginationAttribute>()
		                    .FirstOrDefault();
		if (filter == null)
			throw new GracefulException("Route doesn't have a LinkPaginationAttribute");

		return Paginate(query, predicate, pq, filter.DefaultLimit, filter.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		Expression<Func<T, long>> predicate,
		MastodonPaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var filter = context.ActionDescriptor.FilterDescriptors.Select(p => p.Filter)
		                    .OfType<LinkPaginationAttribute>()
		                    .FirstOrDefault();
		if (filter == null)
			throw new GracefulException("Route doesn't have a LinkPaginationAttribute");

		return Paginate(query, predicate, pq, filter.DefaultLimit, filter.MaxLimit);
	}

	public static IQueryable<T> Paginate<T>(
		this IQueryable<T> query,
		PaginationQuery pq,
		ControllerContext context
	) where T : IEntity
	{
		var filter = context.ActionDescriptor.FilterDescriptors.Select(p => p.Filter)
		                    .OfType<LinkPaginationAttribute>()
		                    .FirstOrDefault();
		if (filter == null)
			throw new GracefulException("Route doesn't have a LinkPaginationAttribute");

		return Paginate(query, pq, filter.DefaultLimit, filter.MaxLimit);
	}

	public static IQueryable<Note> HasVisibility(this IQueryable<Note> query, Note.NoteVisibility visibility)
	{
		return query.Where(note => note.Visibility == visibility);
	}

	public static IQueryable<Note> FilterByFollowingAndOwn(
		this IQueryable<Note> query, User user, DatabaseContext db, int heuristic
	)
	{
		const int cutoff = 250;

		if (heuristic < cutoff)
			return query.Where(note => db.Users
			                             .First(p => p == user)
			                             .Following
			                             .Select(p => p.Id)
			                             .Concat(new[] { user.Id })
			                             .Contains(note.UserId));

		return query.Where(note => note.User == user || note.User.IsFollowedBy(user));
	}

	//TODO: move this into another class where it makes more sense
	public static async Task<int> GetHeuristic(User user, DatabaseContext db, CacheService cache)
	{
		return await cache.FetchValueAsync($"following-query-heuristic:{user.Id}",
		                                   TimeSpan.FromHours(24), FetchHeuristic);

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall")]
		async Task<int> FetchHeuristic()
		{
			var lastDate = await db.Notes.AnyAsync()
				? await db.Notes.OrderByDescending(p => p.Id).Select(p => p.CreatedAt).FirstOrDefaultAsync()
				: DateTime.UtcNow;

			return await db.Notes.CountAsync(p => p.CreatedAt > lastDate - TimeSpan.FromDays(7) &&
			                                      (p.User.IsFollowedBy(user) || p.User == user));
		}
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
		                                                       p.Renote != null &&
		                                                       p.Renote.IsVisibleFor(user),
		                                                       p.Renote != null &&
		                                                       p.Renote.Renote != null &&
		                                                       p.Renote.Renote.IsVisibleFor(user)));
	}

	public static IQueryable<Note> PrecomputeNoteContextVisibilities(this IQueryable<Note> query, User? user)
	{
		return query.Select(p => p.WithPrecomputedVisibilities(false,
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

	public static IQueryable<Note> FilterBlocked(this IQueryable<Note> query, User? user)
	{
		if (user == null) return query;
		return query.Where(note => !note.User.IsBlocking(user) && !note.User.IsBlockedBy(user))
		            .Where(note => note.Renote == null ||
		                           (!note.Renote.User.IsBlockedBy(user) && !note.Renote.User.IsBlocking(user)))
		            .Where(note => note.Renote == null ||
		                           note.Renote.Renote == null ||
		                           (!note.Renote.Renote.User.IsBlockedBy(user) &&
		                            !note.Renote.Renote.User.IsBlocking(user)))
		            .Where(note => note.Reply == null ||
		                           (!note.Reply.User.IsBlockedBy(user) && !note.Reply.User.IsBlocking(user)));
	}

	public static IQueryable<Note> FilterIncomingBlocks(this IQueryable<Note> query, User? user)
	{
		if (user == null) return query;
		return query.Where(note => !note.User.IsBlocking(user))
		            .Where(note => note.Renote == null || !note.Renote.User.IsBlocking(user))
		            .Where(note => note.Renote == null ||
		                           note.Renote.Renote == null ||
		                           !note.Renote.Renote.User.IsBlocking(user))
		            .Where(note => note.Reply == null || !note.Reply.User.IsBlocking(user));
	}

	public static IQueryable<TSource> FilterBlocked<TSource>(
		this IQueryable<TSource> query, Expression<Func<TSource, User?>> predicate, User? user
	)
	{
		return user == null ? query : query.Where(predicate.Compose(p => p == null || !p.IsBlocking(user)));
	}

	public static IQueryable<TSource> FilterBlocked<TSource>(
		this IQueryable<TSource> query, Expression<Func<TSource, Note?>> predicate, User? user
	)
	{
		if (user == null)
			return query;

		return query.Where(predicate.Compose(note => note == null ||
		                                             (!note.User.IsBlocking(user) &&
		                                              !note.User.IsBlockedBy(user) &&
		                                              (note.Renote == null ||
		                                               (!note.Renote.User.IsBlockedBy(user) &&
		                                                !note.Renote.User.IsBlocking(user))) &&
		                                              (note.Renote == null ||
		                                               note.Renote.Renote == null ||
		                                               (!note.Renote.Renote.User.IsBlockedBy(user) &&
		                                                !note.Renote.Renote.User.IsBlocking(user))) &&
		                                              (note.Reply == null ||
		                                               (!note.Reply.User.IsBlockedBy(user) &&
		                                                !note.Reply.User.IsBlocking(user))))));
	}

	public static IQueryable<Note> FilterMuted(this IQueryable<Note> query, User? user)
	{
		//TODO: handle muted instances
		if (user == null) return query;

		return query.Where(note => !note.User.IsMutedBy(user))
		            .Where(note => note.Renote == null || !note.Renote.User.IsMutedBy(user))
		            .Where(note => note.Renote == null ||
		                           note.Renote.Renote == null ||
		                           !note.Renote.Renote.User.IsMutedBy(user))
		            .Where(note => note.Reply == null || !note.Reply.User.IsMutedBy(user));
	}

	public static IQueryable<Note> FilterBlockedConversations(
		this IQueryable<Note> query, User user, DatabaseContext db
	)
	{
		return query.Where(p => !db.Blockings.Any(i => i.Blocker == user && p.VisibleUserIds.Contains(i.BlockeeId)));
	}

	public static IQueryable<Note> FilterMutedConversations(this IQueryable<Note> query, User user, DatabaseContext db)
	{
		//TODO: handle muted instances

		return query.Where(p => !db.Mutings.Any(i => i.Muter == user && p.VisibleUserIds.Contains(i.MuteeId)));
	}

	public static IQueryable<Note> FilterHiddenListMembers(this IQueryable<Note> query, User user)
	{
		return query.Where(note => !note.User.UserListMembers.Any(p => p.UserList.User == user &&
		                                                               p.UserList.HideFromHomeTl));
	}

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
		this IQueryable<Note> notes, NoteRenderer renderer, User? user
	)
	{
		var list = (await notes.ToListAsync())
		           .EnforceRenoteReplyVisibility()
		           .ToList();
		return (await renderer.RenderManyAsync(list, user)).ToList();
	}

	public static async Task<List<AccountEntity>> RenderAllForMastodonAsync(
		this IQueryable<User> users, UserRenderer renderer
	)
	{
		var list = await users.ToListAsync();
		return (await renderer.RenderManyAsync(list)).ToList();
	}

	public static async Task<List<NotificationEntity>> RenderAllForMastodonAsync(
		this IQueryable<Notification> notifications, NotificationRenderer renderer, User user
	)
	{
		var list = (await notifications.ToListAsync())
		           .EnforceRenoteReplyVisibility(p => p.Note)
		           .ToList();
		return (await renderer.RenderManyAsync(list, user)).ToList();
	}

	public static IQueryable<Note> FilterByAccountStatusesRequest(
		this IQueryable<Note> query, AccountSchemas.AccountStatusesRequest request
	)
	{
		if (request.ExcludeReplies)
			query = query.Where(p => p.Reply == null);
		if (request.ExcludeRenotes)
			query = query.Where(p => p.Renote == null);
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

	public static IQueryable<FollowRequest> IncludeCommonProperties(this IQueryable<FollowRequest> query)
	{
		return query.Include(p => p.Follower.UserProfile)
		            .Include(p => p.Followee.UserProfile);
	}

	public static IQueryable<Notification> IncludeCommonProperties(this IQueryable<Notification> query)
	{
		return query.Include(p => p.Notifiee.UserProfile)
		            .Include(p => p.Notifier.UserProfile)
		            .Include(p => p.Note.User.UserProfile)
		            .Include(p => p.Note.Renote.User.UserProfile)
		            .Include(p => p.Note.Reply.User.UserProfile)
		            .Include(p => p.FollowRequest.Follower.UserProfile)
		            .Include(p => p.FollowRequest.Followee.UserProfile);
	}

#pragma warning restore CS8602 // Dereference of a possibly null reference.
}