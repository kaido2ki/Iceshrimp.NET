using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Parsing;
using Microsoft.EntityFrameworkCore;
using static Iceshrimp.Parsing.SearchQueryFilters;

namespace Iceshrimp.Backend.Core.Extensions;

public static class QueryableFtsExtensions
{
	public static IQueryable<Note> FilterByFtsQuery(
		this IQueryable<Note> query, string input, User user, Config.InstanceSection config, DatabaseContext db
	)
	{
		var parsed          = SearchQuery.parse(input);
		var caseSensitivity = parsed.OfType<CaseFilter>().LastOrDefault()?.Value ?? CaseFilterType.Insensitive;
		var matchType       = parsed.OfType<MatchFilter>().LastOrDefault()?.Value ?? MatchFilterType.Substring;

		query = query.ApplyFromFilters(parsed.OfType<FromFilter>().ToList(), config, db);

		return parsed.Aggregate(query, (current, filter) => filter switch
		{
			CaseFilter                        => current,
			MatchFilter                       => current,
			FromFilter                        => current,
			AfterFilter afterFilter           => current.ApplyAfterFilter(afterFilter),
			AttachmentFilter attachmentFilter => current.ApplyAttachmentFilter(attachmentFilter),
			BeforeFilter beforeFilter         => current.ApplyBeforeFilter(beforeFilter),
			InFilter inFilter                 => current.ApplyInFilter(inFilter, user, db),
			InstanceFilter instanceFilter     => current.ApplyInstanceFilter(instanceFilter, config),
			MentionFilter mentionFilter       => current.ApplyMentionFilter(mentionFilter, config, db),
			MiscFilter miscFilter             => current.ApplyMiscFilter(miscFilter, user),
			ReplyFilter replyFilter           => current.ApplyReplyFilter(replyFilter, config, db),
			WordFilter wordFilter             => current.ApplyWordFilter(wordFilter, caseSensitivity, matchType),
			MultiWordFilter multiWordFilter =>
				current.ApplyMultiWordFilter(multiWordFilter, caseSensitivity, matchType),
			_ => throw new ArgumentOutOfRangeException(nameof(filter))
		});
	}

	internal static (string username, string? host) UserToTuple(string filter, Config.InstanceSection config)
	{
		filter = filter.TrimStart('@');
		var split    = filter.Split('@');
		var username = split[0].ToLowerInvariant();
		var host     = split.Length > 1 ? split[1] : null;
		return (username, LocalDomainCheck(host, config));
	}

	/// <returns>
	/// The input variable host, or null if it matches the configured web or account domain.
	/// </returns>
	internal static string? LocalDomainCheck(string? host, Config.InstanceSection config) =>
		host == null || host == config.WebDomain || host == config.AccountDomain ? null : host;

	[Projectable]
	private static IQueryable<Note> ApplyAfterFilter(this IQueryable<Note> query, AfterFilter filter)
		=> query.Where(p => p.CreatedAt >= filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

	[Projectable]
	private static IQueryable<Note> ApplyBeforeFilter(this IQueryable<Note> query, BeforeFilter filter)
		=> query.Where(p => p.CreatedAt < filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

	[Projectable]
	private static IQueryable<Note> ApplyWordFilter(
		this IQueryable<Note> query, WordFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => query.Where(p => p.FtsQueryPreEscaped(PreEscapeFtsQuery(filter.Value, matchType), filter.Negated,
	                                           caseSensitivity, matchType));

	[Projectable]
	private static IQueryable<Note> ApplyMultiWordFilter(
		this IQueryable<Note> query, MultiWordFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => query.Where(p => p.FtsQueryOneOf(filter.Values, caseSensitivity, matchType));

	private static IQueryable<Note> ApplyFromFilters(
		this IQueryable<Note> query, List<FromFilter> filters, Config.InstanceSection config, DatabaseContext db
	)
	{
		if (filters.Count == 0) return query;
		var expr = ExpressionExtensions.False<Note>();
		expr = filters.Aggregate(expr, (current, filter) => current
			                         .Or(p => p.User.UserSubqueryMatches(filter.Value, filter.Negated, config, db)));
		return query.Where(expr);
	}

	[Projectable]
	private static IQueryable<Note> ApplyInstanceFilter(
		this IQueryable<Note> query, InstanceFilter filter, Config.InstanceSection config
	) => query.Where(p => filter.Negated
		                 ? p.UserHost != LocalDomainCheck(filter.Value, config)
		                 : p.UserHost == LocalDomainCheck(filter.Value, config));

	[Projectable]
	private static IQueryable<Note> ApplyMentionFilter(
		this IQueryable<Note> query, MentionFilter filter, Config.InstanceSection config, DatabaseContext db
	) => query.Where(p => p.Mentions.UserSubqueryContains(filter.Value, filter.Negated, config, db));

	[Projectable]
	private static IQueryable<Note> ApplyReplyFilter(
		this IQueryable<Note> query, ReplyFilter filter, Config.InstanceSection config, DatabaseContext db
	) => query.Where(p => p.Reply != null &&
	                      p.Reply.User.UserSubqueryMatches(filter.Value, filter.Negated, config, db));

	[Projectable]
	private static IQueryable<Note> ApplyInFilter(
		this IQueryable<Note> query, InFilter filter, User user, DatabaseContext db
	) => filter.Value.Equals(InFilterType.Bookmarks)
		? query.ApplyInBookmarksFilter(user, filter.Negated, db)
		: filter.Value.Equals(InFilterType.Likes)
			? query.ApplyInLikesFilter(user, filter.Negated, db)
			: filter.Value.Equals(InFilterType.Reactions)
				? query.ApplyInReactionsFilter(user, filter.Negated, db)
				: query.ApplyInInteractionsFilter(user, filter.Negated, db);

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyInBookmarksFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasBookmarked(p)
		                 : db.Users.First(u => u == user).HasBookmarked(p));

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyInLikesFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasLiked(p)
		                 : db.Users.First(u => u == user).HasLiked(p));

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyInReactionsFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasReacted(p)
		                 : db.Users.First(u => u == user).HasReacted(p));

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyInInteractionsFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasInteractedWith(p)
		                 : db.Users.First(u => u == user).HasInteractedWith(p));

	[Projectable]
	private static IQueryable<Note> ApplyMiscFilter(
		this IQueryable<Note> query, MiscFilter filter, User user
	) => filter.Value.Equals(MiscFilterType.Followers)
		? query.ApplyFollowersFilter(user, filter.Negated)
		: filter.Value.Equals(MiscFilterType.Following)
			? query.ApplyFollowingFilter(user, filter.Negated)
			: filter.Value.Equals(MiscFilterType.Replies)
				? query.ApplyRepliesFilter(filter.Negated)
				: query.ApplyBoostsFilter(filter.Negated);

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyFollowersFilter(
		this IQueryable<Note> query, User user, bool negated
	) => query.Where(p => negated
		                 ? !p.User.IsFollowing(user)
		                 : p.User.IsFollowing(user));

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyFollowingFilter(
		this IQueryable<Note> query, User user, bool negated
	) => query.Where(p => negated
		                 ? !p.User.IsFollowedBy(user)
		                 : p.User.IsFollowedBy(user));

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyRepliesFilter(
		this IQueryable<Note> query, bool negated
	) => query.Where(p => negated
		                 ? p.Reply == null
		                 : p.Reply != null);

	[Projectable]
	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	internal static IQueryable<Note> ApplyBoostsFilter(
		this IQueryable<Note> query, bool negated
	) => query.Where(p => negated
		                 ? !p.IsPureRenote
		                 : p.IsPureRenote);

	[Projectable]
	private static IQueryable<Note> ApplyAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
		=> filter.Negated ? query.ApplyNegatedAttachmentFilter(filter) : query.ApplyRegularAttachmentFilter(filter);

	[Projectable]
	internal static IQueryable<Note> ApplyRegularAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
		=> AttachmentQuery(filter.Value)
			? filter.Value.Equals(AttachmentFilterType.Poll)
				? query.Where(p => p.HasPoll)
				: query.Where(p => EF.Functions.ILike(p.RawAttachments, GetAttachmentILikeQuery(filter.Value)))
			: filter.Value.Equals(AttachmentFilterType.Any)
				? query.Where(p => p.AttachedFileTypes.Count != 0)
				: query.Where(p => p.AttachedFileTypes.Count != 0 &&
				                   (!EF.Functions.ILike(p.RawAttachments,
				                                        GetAttachmentILikeQuery(AttachmentFilterType.Image)) ||
				                    !EF.Functions.ILike(p.RawAttachments,
				                                        GetAttachmentILikeQuery(AttachmentFilterType.Video)) ||
				                    !EF.Functions.ILike(p.RawAttachments,
				                                        GetAttachmentILikeQuery(AttachmentFilterType.Audio))));

	[Projectable]
	internal static IQueryable<Note> ApplyNegatedAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
		=> AttachmentQuery(filter.Value)
			? filter.Value.Equals(AttachmentFilterType.Poll)
				? query.Where(p => !p.HasPoll)
				: query.Where(p => !EF.Functions.ILike(p.RawAttachments, GetAttachmentILikeQuery(filter.Value)))
			: filter.Value.Equals(AttachmentFilterType.Any)
				? query.Where(p => p.AttachedFileTypes.Count == 0)
				: query.Where(p => EF.Functions
				                     .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Image)) ||
				                   EF.Functions
				                     .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Video)) ||
				                   EF.Functions
				                     .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Audio)));

	internal static bool AttachmentQuery(AttachmentFilterType filter)
	{
		if (filter.Equals(AttachmentFilterType.Image))
			return true;
		if (filter.Equals(AttachmentFilterType.Video))
			return true;
		if (filter.Equals(AttachmentFilterType.Audio))
			return true;
		if (filter.Equals(AttachmentFilterType.Poll))
			return true;
		return false;
	}

	internal static string GetAttachmentILikeQuery(AttachmentFilterType filter)
	{
		if (filter.Equals(AttachmentFilterType.Image))
			return "%image/%";
		if (filter.Equals(AttachmentFilterType.Video))
			return "%video/%";
		if (filter.Equals(AttachmentFilterType.Audio))
			return "%audio/%";
		throw new ArgumentOutOfRangeException(nameof(filter));
	}

	[Projectable]
	internal static bool UserSubqueryMatches(
		this User user, string filter, bool negated, Config.InstanceSection config, DatabaseContext db
	) => negated
		? !UserSubquery(UserToTuple(filter, config), db).Contains(user)
		: UserSubquery(UserToTuple(filter, config), db).Contains(user);

	[Projectable]
	internal static bool UserSubqueryContains(
		this IEnumerable<string> userIds, string filter, bool negated, Config.InstanceSection config, DatabaseContext db
	) => negated
		? userIds.All(p => p != UserSubquery(UserToTuple(filter, config), db).Select(i => i.Id).FirstOrDefault())
		: userIds.Any(p => p == UserSubquery(UserToTuple(filter, config), db).Select(i => i.Id).FirstOrDefault());

	[Projectable]
	internal static IQueryable<User> UserSubquery((string username, string? host) filter, DatabaseContext db) =>
		db.Users.Where(p => p.UsernameLower == filter.username && p.Host == filter.host);

	[Projectable]
	internal static bool FtsQueryPreEscaped(
		this Note note, string query, bool negated, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => matchType.Equals(MatchFilterType.Substring)
		? caseSensitivity.Equals(CaseFilterType.Sensitive)
			? negated
				? !EF.Functions.Like(note.Text!, "%" + query + "%", @"\") &&
				  !EF.Functions.Like(note.Cw!, "%" + query + "%", @"\")
				: EF.Functions.Like(note.Text!, "%" + query + "%", @"\") ||
				  EF.Functions.Like(note.Cw!, "%" + query + "%", @"\")
			: negated
				? !EF.Functions.ILike(note.Text!, "%" + query + "%", @"\") &&
				  !EF.Functions.ILike(note.Cw!, "%" + query + "%", @"\")
				: EF.Functions.ILike(note.Text!, "%" + query + "%", @"\") ||
				  EF.Functions.ILike(note.Cw!, "%" + query + "%", @"\")
		: caseSensitivity.Equals(CaseFilterType.Sensitive)
			? negated
				? !Regex.IsMatch(note.Text!, "\\y" + query + "\\y") &&
				  !Regex.IsMatch(note.Cw!, "\\y" + query + "\\y")
				: Regex.IsMatch(note.Text!, "\\y" + query + "\\y") ||
				  Regex.IsMatch(note.Cw!, "\\y" + query + "\\y")
			: negated
				? !Regex.IsMatch(note.Text!, "\\y" + query + "\\y", RegexOptions.IgnoreCase) &&
				  !Regex.IsMatch(note.Cw!, "\\y" + query + "\\y", RegexOptions.IgnoreCase)
				: Regex.IsMatch(note.Text!, "\\y" + query + "\\y", RegexOptions.IgnoreCase) ||
				  Regex.IsMatch(note.Cw!, "\\y" + query + "\\y", RegexOptions.IgnoreCase);

	internal static string PreEscapeFtsQuery(string query, MatchFilterType matchType)
		=> matchType.Equals(MatchFilterType.Substring)
			? EfHelpers.EscapeLikeQuery(query)
			: EfHelpers.EscapeRegexQuery(query);

	[Projectable]
	internal static bool FtsQueryOneOf(
		this Note note, IEnumerable<string> words, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => words.Select(p => PreEscapeFtsQuery(p, matchType))
	          .Any(p => note.FtsQueryPreEscaped(p, false, caseSensitivity, matchType));
}