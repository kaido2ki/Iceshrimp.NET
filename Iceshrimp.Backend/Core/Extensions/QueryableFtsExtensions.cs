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

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static (string username, string? host) UserToTuple(string filter, Config.InstanceSection config)
	{
		filter = filter.TrimStart('@');
		var split    = filter.Split('@');
		var username = split[0].ToLowerInvariant();
		var host     = split.Length > 1 ? split[1] : null;
		return (username, LocalDomainCheck(host, config));
	}

	/// <returns>
	///     The input variable host, or null if it matches the configured web or account domain.
	/// </returns>
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static string? LocalDomainCheck(string? host, Config.InstanceSection config) =>
		host == null || host == config.WebDomain || host == config.AccountDomain ? null : host;

	private static IQueryable<Note> ApplyAfterFilter(this IQueryable<Note> query, AfterFilter filter)
		=> query.Where(p => p.CreatedAt >= filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

	private static IQueryable<Note> ApplyBeforeFilter(this IQueryable<Note> query, BeforeFilter filter)
		=> query.Where(p => p.CreatedAt < filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

	private static IQueryable<Note> ApplyWordFilter(
		this IQueryable<Note> query, WordFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => query.Where(p => p.FtsQueryPreEscaped(PreEscapeFtsQuery(filter.Value, matchType), filter.Negated,
	                                           caseSensitivity, matchType));

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

	private static IQueryable<Note> ApplyInstanceFilter(
		this IQueryable<Note> query, InstanceFilter filter, Config.InstanceSection config
	) => query.Where(p => filter.Negated
		                 ? p.UserHost != LocalDomainCheck(filter.Value, config)
		                 : p.UserHost == LocalDomainCheck(filter.Value, config));

	private static IQueryable<Note> ApplyMentionFilter(
		this IQueryable<Note> query, MentionFilter filter, Config.InstanceSection config, DatabaseContext db
	) => query.Where(p => p.Mentions.UserSubqueryContains(filter.Value, filter.Negated, config, db));

	private static IQueryable<Note> ApplyReplyFilter(
		this IQueryable<Note> query, ReplyFilter filter, Config.InstanceSection config, DatabaseContext db
	) => query.Where(p => p.Reply != null &&
	                      p.Reply.User.UserSubqueryMatches(filter.Value, filter.Negated, config, db));

	private static IQueryable<Note> ApplyInFilter(
		this IQueryable<Note> query, InFilter filter, User user, DatabaseContext db
	)
	{
		return filter.Value switch
		{
			{ IsLikes: true }        => query.ApplyInLikesFilter(user, filter.Negated, db),
			{ IsBookmarks: true }    => query.ApplyInBookmarksFilter(user, filter.Negated, db),
			{ IsReactions: true }    => query.ApplyInReactionsFilter(user, filter.Negated, db),
			{ IsInteractions: true } => query.ApplyInInteractionsFilter(user, filter.Negated, db),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), filter.Value, null)
		};
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyInBookmarksFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasBookmarked(p)
		                 : db.Users.First(u => u == user).HasBookmarked(p));

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyInLikesFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasLiked(p)
		                 : db.Users.First(u => u == user).HasLiked(p));

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyInReactionsFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasReacted(p)
		                 : db.Users.First(u => u == user).HasReacted(p));

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyInInteractionsFilter(
		this IQueryable<Note> query, User user, bool negated, DatabaseContext db
	) => query.Where(p => negated
		                 ? !db.Users.First(u => u == user).HasInteractedWith(p)
		                 : db.Users.First(u => u == user).HasInteractedWith(p));

	private static IQueryable<Note> ApplyMiscFilter(this IQueryable<Note> query, MiscFilter filter, User user)
	{
		return filter.Value switch
		{
			{ IsFollowers: true } => query.ApplyFollowersFilter(user, filter.Negated),
			{ IsFollowing: true } => query.ApplyFollowingFilter(user, filter.Negated),
			{ IsRenotes: true }   => query.ApplyBoostsFilter(filter.Negated),
			{ IsReplies: true }   => query.ApplyRepliesFilter(filter.Negated),
			_                     => throw new ArgumentOutOfRangeException(nameof(filter))
		};
	}

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyFollowersFilter(this IQueryable<Note> query, User user, bool negated)
		=> query.Where(p => negated ? !p.User.IsFollowing(user) : p.User.IsFollowing(user));

	[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
	private static IQueryable<Note> ApplyFollowingFilter(this IQueryable<Note> query, User user, bool negated)
		=> query.Where(p => negated ? !p.User.IsFollowedBy(user) : p.User.IsFollowedBy(user));

	private static IQueryable<Note> ApplyRepliesFilter(this IQueryable<Note> query, bool negated)
		=> query.Where(p => negated ? p.Reply == null : p.Reply != null);

	private static IQueryable<Note> ApplyBoostsFilter(this IQueryable<Note> query, bool negated)
		=> query.Where(p => negated ? !p.IsPureRenote : p.IsPureRenote);

	private static IQueryable<Note> ApplyAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
		=> filter.Negated ? query.ApplyNegatedAttachmentFilter(filter) : query.ApplyRegularAttachmentFilter(filter);

	private static IQueryable<Note> ApplyRegularAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
	{
		if (filter.Value.IsAny)
			return query.Where(p => p.AttachedFileTypes.Count != 0);
		if (filter.Value.IsPoll)
			return query.Where(p => p.HasPoll);

		if (filter.Value.IsImage || filter.Value.IsVideo || filter.Value.IsAudio)
		{
			return query.Where(p => p.AttachedFileTypes.Count != 0 &&
			                        EF.Functions.ILike(p.RawAttachments, GetAttachmentILikeQuery(filter.Value)));
		}

		if (filter.Value.IsFile)
		{
			return query.Where(p => p.AttachedFileTypes.Count != 0 &&
			                        (!EF.Functions.ILike(p.RawAttachments,
			                                             GetAttachmentILikeQuery(AttachmentFilterType.Image)) ||
			                         !EF.Functions.ILike(p.RawAttachments,
			                                             GetAttachmentILikeQuery(AttachmentFilterType.Video)) ||
			                         !EF.Functions.ILike(p.RawAttachments,
			                                             GetAttachmentILikeQuery(AttachmentFilterType.Audio))));
		}

		throw new ArgumentOutOfRangeException(nameof(filter), filter.Value, null);
	}

	private static IQueryable<Note> ApplyNegatedAttachmentFilter(this IQueryable<Note> query, AttachmentFilter filter)
	{
		if (filter.Value.IsAny)
			return query.Where(p => p.AttachedFileTypes.Count == 0);
		if (filter.Value.IsPoll)
			return query.Where(p => !p.HasPoll);
		if (filter.Value.IsImage || filter.Value.IsVideo || filter.Value.IsAudio)
			return query.Where(p => !EF.Functions.ILike(p.RawAttachments, GetAttachmentILikeQuery(filter.Value)));

		if (filter.Value.IsFile)
		{
			return query.Where(p => EF.Functions
			                          .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Image)) ||
			                        EF.Functions
			                          .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Video)) ||
			                        EF.Functions
			                          .ILike(p.RawAttachments, GetAttachmentILikeQuery(AttachmentFilterType.Audio)));
		}

		throw new ArgumentOutOfRangeException(nameof(filter), filter.Value, null);
	}

	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static string GetAttachmentILikeQuery(AttachmentFilterType filter)
	{
		return filter switch
		{
			{ IsImage: true } => "%image/%",
			{ IsVideo: true } => "%video/%",
			{ IsAudio: true } => "%audio/%",
			_                 => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
		};
	}

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool UserSubqueryMatches(
		this User user, string filter, bool negated, Config.InstanceSection config, DatabaseContext db
	) => negated
		? !UserSubquery(UserToTuple(filter, config), db).Contains(user)
		: UserSubquery(UserToTuple(filter, config), db).Contains(user);

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool UserSubqueryContains(
		this IEnumerable<string> userIds, string filter, bool negated, Config.InstanceSection config, DatabaseContext db
	) => negated
		? userIds.All(p => p != UserSubquery(UserToTuple(filter, config), db).Select(i => i.Id).FirstOrDefault())
		: userIds.Any(p => p == UserSubquery(UserToTuple(filter, config), db).Select(i => i.Id).FirstOrDefault());

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static IQueryable<User> UserSubquery((string username, string? host) filter, DatabaseContext db) =>
		db.Users.Where(p => p.UsernameLower == filter.username &&
		                    p.Host == (filter.host != null ? filter.host.ToPunycodeLower() : null));

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
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
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool FtsQueryOneOf(
		this Note note, IEnumerable<string> words, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => words.Select(p => PreEscapeFtsQuery(p, matchType))
	          .Any(p => note.FtsQueryPreEscaped(p, false, caseSensitivity, matchType));
}