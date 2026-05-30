using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using EntityFrameworkCore.Projectables;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.EntityFrameworkCore.Extensions;
using Iceshrimp.Parsing;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class QueryableFtsExtensions
{
	public static IQueryable<Note> FilterByFtsQuery(
		this IQueryable<Note> query, string input, User user, Config.InstanceSection config, DatabaseContext db
	)
	{
		var parsed          = SearchQueryParser.Parse(input);
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
			VisibilityFilter visibilityFilter => current.ApplyVisibilityFilter(visibilityFilter),
			ReplyFilter replyFilter           => current.ApplyReplyFilter(replyFilter, config, db),
			CwFilter cwFilter                 => current.ApplyCwFilter(cwFilter, caseSensitivity, matchType),
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

	extension(IQueryable<Note> query)
	{
		private IQueryable<Note> ApplyAfterFilter(AfterFilter filter)
			=> query.Where(p => p.CreatedAt >= filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

		private IQueryable<Note> ApplyBeforeFilter(BeforeFilter filter)
			=> query.Where(p => p.CreatedAt < filter.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

		private IQueryable<Note> ApplyWordFilter(
			WordFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
		) => query.Where(p => p.FtsQueryPreEscaped(PreEscapeFtsQuery(filter.Value, matchType), filter.Negated,
		                                           caseSensitivity, matchType));

		private IQueryable<Note> ApplyCwFilter(
			CwFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
		) => query.Where(p => FtsQueryPreEscapedMatch(p.Cw, PreEscapeFtsQuery(filter.Value, matchType), filter.Negated,
		                                              caseSensitivity, matchType));

		private IQueryable<Note> ApplyMultiWordFilter(
			MultiWordFilter filter, CaseFilterType caseSensitivity, MatchFilterType matchType
		) => filter.Negated
			? query.Where(p => !p.FtsQueryOneOf(filter.Values, caseSensitivity, matchType))
			: query.Where(p => p.FtsQueryOneOf(filter.Values, caseSensitivity, matchType));

		private IQueryable<Note> ApplyFromFilters(
			List<FromFilter> filters, Config.InstanceSection config, DatabaseContext db
		)
		{
			if (filters.Count == 0) return query;
			var expr = ExpressionExtensions.False<Note>();
			expr = filters.Aggregate(expr, (current, filter) => current
				                         .Or(p => p.User.UserSubqueryMatches(filter.Value, filter.Negated, config, db)));
			return query.Where(expr);
		}

		private IQueryable<Note> ApplyInstanceFilter(
			InstanceFilter filter, Config.InstanceSection config
		) => query.Where(p => filter.Negated
			                 ? p.UserHost != LocalDomainCheck(filter.Value, config)
			                 : p.UserHost == LocalDomainCheck(filter.Value, config));

		private IQueryable<Note> ApplyMentionFilter(
			MentionFilter filter, Config.InstanceSection config, DatabaseContext db
		) => query.Where(p => p.Mentions.UserSubqueryContains(filter.Value, filter.Negated, config, db));

		private IQueryable<Note> ApplyReplyFilter(
			ReplyFilter filter, Config.InstanceSection config, DatabaseContext db
		) => query.Where(p => p.Reply != null
		                      && p.Reply.User.UserSubqueryMatches(filter.Value, filter.Negated, config, db));

		private IQueryable<Note> ApplyInFilter(
			InFilter filter, User user, DatabaseContext db
		)
		{
			return filter.Value switch
			{
				InFilterType.Likes        => query.ApplyInLikesFilter(user, filter.Negated, db),
				InFilterType.Bookmarks    => query.ApplyInBookmarksFilter(user, filter.Negated, db),
				InFilterType.Reactions    => query.ApplyInReactionsFilter(user, filter.Negated, db),
				InFilterType.Interactions => query.ApplyInInteractionsFilter(user, filter.Negated, db),
				_                         => throw new ArgumentOutOfRangeException(nameof(filter), filter.Value, null)
			};
		}

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyInBookmarksFilter(
			User user, bool negated, DatabaseContext db
		) => query.Where(p => negated
			                 ? !db.Users.First(u => u == user).HasBookmarked(p)
			                 : db.Users.First(u => u == user).HasBookmarked(p));

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyInLikesFilter(
			User user, bool negated, DatabaseContext db
		) => query.Where(p => negated
			                 ? !db.Users.First(u => u == user).HasLiked(p)
			                 : db.Users.First(u => u == user).HasLiked(p));

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyInReactionsFilter(
			User user, bool negated, DatabaseContext db
		) => query.Where(p => negated
			                 ? !db.Users.First(u => u == user).HasReacted(p)
			                 : db.Users.First(u => u == user).HasReacted(p));

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyInInteractionsFilter(
			User user, bool negated, DatabaseContext db
		) => query.Where(p => negated
			                 ? !db.Users.First(u => u == user).HasInteractedWith(p)
			                 : db.Users.First(u => u == user).HasInteractedWith(p));

		private IQueryable<Note> ApplyMiscFilter(MiscFilter filter, User user)
		{
			return filter.Value switch
			{
				MiscFilterType.Followers => query.ApplyFollowersFilter(user, filter.Negated),
				MiscFilterType.Following => query.ApplyFollowingFilter(user, filter.Negated),
				MiscFilterType.Renotes   => query.ApplyBoostsFilter(filter.Negated),
				MiscFilterType.Replies   => query.ApplyRepliesFilter(filter.Negated),
				_                        => throw new ArgumentOutOfRangeException(nameof(filter))
			};
		}

		private IQueryable<Note> ApplyVisibilityFilter(VisibilityFilter filter)
		{
			if (filter.Value is VisibilityFilterType.Local)
				return query.Where(p => p.LocalOnly == !filter.Negated);

			var visibility = filter.Value switch
			{
				VisibilityFilterType.Public    => Note.NoteVisibility.Public,
				VisibilityFilterType.Home      => Note.NoteVisibility.Home,
				VisibilityFilterType.Followers => Note.NoteVisibility.Followers,
				VisibilityFilterType.Specified => Note.NoteVisibility.Specified,
				_                              => throw new ArgumentOutOfRangeException()
			};

			return filter.Negated
				? query.Where(p => p.Visibility != visibility)
				: query.Where(p => p.Visibility == visibility);
		}

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyFollowersFilter(User user, bool negated)
			=> query.Where(p => negated ? !p.User.IsFollowing(user) : p.User.IsFollowing(user));

		[SuppressMessage("ReSharper", "EntityFramework.UnsupportedServerSideFunctionCall", Justification = "Projectables")]
		private IQueryable<Note> ApplyFollowingFilter(User user, bool negated)
			=> query.Where(p => negated ? !p.User.IsFollowedBy(user) : p.User.IsFollowedBy(user));

		private IQueryable<Note> ApplyRepliesFilter(bool negated)
			=> query.Where(p => negated ? p.Reply == null : p.Reply != null);

		private IQueryable<Note> ApplyBoostsFilter(bool negated)
			=> query.Where(p => negated ? !p.IsPureRenote : p.IsPureRenote);
		
		private IQueryable<Note> ApplyImageFilter(bool negated)
		{
			return negated 
				? query.Where(p => !p.AttachedFileTypes.Any(m => m.StartsWith("image/")))
			    : query.Where(p => p.AttachedFileTypes.Count != 0
			                        && p.AttachedFileTypes.Any(m => m.StartsWith("image/")));
		}
		
		private IQueryable<Note> ApplyAudioFilter(bool negated)
		{
			return negated
				? query.Where(p => !p.AttachedFileTypes.Any(m => m.StartsWith("audio/")))
				: query.Where(p => p.AttachedFileTypes.Count != 0
				                   && p.AttachedFileTypes.Any(m => m.StartsWith("audio/")));
		}
		
		private IQueryable<Note> ApplyVideoFilter(bool negated)
		{
			return negated
				? query.Where(p => !p.AttachedFileTypes.Any(m => m.StartsWith("video/")))
				: query.Where(p => p.AttachedFileTypes.Count != 0
				                   && p.AttachedFileTypes.Any(m => m.StartsWith("video/")));
		}
		
		private IQueryable<Note> ApplyFileFilter(bool negated)
		{
			return query.Where(p => negated
			                   ? p.AttachedFileTypes.Count == 0
							     || p.AttachedFileTypes.Any(m => m.StartsWith("image/")
							                                     || m.StartsWith("audio/")
							                                     || m.StartsWith("video/"))
			                   : p.AttachedFileTypes.Count != 0
			                     && !p.AttachedFileTypes.Any(m => m.StartsWith("image/")
			                                                      || m.StartsWith("audio/")
			                                                      || m.StartsWith("video/")));
		}

		private IQueryable<Note> ApplyAttachmentFilter(AttachmentFilter filter)
		{
			return filter.Value switch
			{
				AttachmentFilterType.Media => query.Where(p => filter.Negated
					                                          ? p.AttachedFileTypes.Count == 0 
					                                          : p.AttachedFileTypes.Count != 0),
				AttachmentFilterType.Poll  => query.Where(p => filter.Negated ? !p.HasPoll : p.HasPoll),
				AttachmentFilterType.Image => query.ApplyImageFilter(filter.Negated),
				AttachmentFilterType.Audio => query.ApplyAudioFilter(filter.Negated),
				AttachmentFilterType.Video => query.ApplyVideoFilter(filter.Negated),
				AttachmentFilterType.File  => query.ApplyFileFilter(filter.Negated),
				_                          => throw new ArgumentOutOfRangeException(nameof(filter), filter.Value, null)
			};
		}
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
		db.Users.Where(p => p.UsernameLower == filter.username
		                    && p.Host == (filter.host != null ? filter.host.ToPunycodeLower() : null));

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool FtsQueryPreEscaped(
		this Note note, string query, bool negated, CaseFilterType caseSensitivity, MatchFilterType matchType
	) => negated
		? matchType.Equals(MatchFilterType.Substring)
			? FtsQueryMatchSubstringPreEscaped(note.Text, query, negated, caseSensitivity)
			  && FtsQueryMatchSubstringPreEscaped(note.Cw, query, negated, caseSensitivity)
			  && FtsQueryMatchSubstringPreEscaped(note.CombinedAltText, query, negated, caseSensitivity)
			: FtsQueryMatchWordPreEscaped(note.Text, query, negated, caseSensitivity)
			  && FtsQueryMatchWordPreEscaped(note.Cw, query, negated, caseSensitivity)
			  && FtsQueryMatchWordPreEscaped(note.CombinedAltText, query, negated, caseSensitivity)
		: matchType.Equals(MatchFilterType.Substring)
			? FtsQueryMatchSubstringPreEscaped(note.Text, query, negated, caseSensitivity)
			  || FtsQueryMatchSubstringPreEscaped(note.Cw, query, negated, caseSensitivity)
			  || FtsQueryMatchSubstringPreEscaped(note.CombinedAltText, query, negated, caseSensitivity)
			: FtsQueryMatchWordPreEscaped(note.Text, query, negated, caseSensitivity)
			  || FtsQueryMatchWordPreEscaped(note.Cw, query, negated, caseSensitivity)
			  || FtsQueryMatchWordPreEscaped(note.CombinedAltText, query, negated, caseSensitivity);

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool FtsQueryPreEscapedMatch(
		string? match, string query, bool negated,
		CaseFilterType caseSensitivity, MatchFilterType matchType
	) => matchType.Equals(MatchFilterType.Substring)
		? caseSensitivity.Equals(CaseFilterType.Sensitive)
			? negated
				? !EF.Functions.Like(match!, "%" + query + "%", @"\")
				: EF.Functions.Like(match!, "%" + query + "%", @"\")
			: negated
				? !EF.Functions.ILike(match!, "%" + query + "%", @"\")
				: EF.Functions.ILike(match!, "%" + query + "%", @"\")
		: caseSensitivity.Equals(CaseFilterType.Sensitive)
			? negated
				? !Regex.IsMatch(match!, "\\y" + query + "\\y")
				: Regex.IsMatch(match!, "\\y" + query + "\\y")
			: negated
				? !Regex.IsMatch(match!, "\\y" + query + "\\y", RegexOptions.IgnoreCase)
				: Regex.IsMatch(match!, "\\y" + query + "\\y", RegexOptions.IgnoreCase);

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

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool FtsQueryMatchSubstringPreEscaped(
		string? match, string query, bool negated, CaseFilterType caseSensitivity
	) => caseSensitivity.Equals(CaseFilterType.Sensitive)
		? negated
			? !EF.Functions.Like(match!, "%" + query + "%", @"\")
			: EF.Functions.Like(match!, "%" + query + "%", @"\")
		: negated
			? !EF.Functions.ILike(match!, "%" + query + "%", @"\")
			: EF.Functions.ILike(match!, "%" + query + "%", @"\");

	[Projectable]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global",
	                 Justification = "Projectable chain must have consistent visibility")]
	internal static bool FtsQueryMatchWordPreEscaped(
		string? match, string query, bool negated, CaseFilterType caseSensitivity
	) => caseSensitivity.Equals(CaseFilterType.Sensitive)
		? negated
			? match == null || !Regex.IsMatch(match, "\\y" + query + "\\y")
			: match != null && Regex.IsMatch(match, "\\y" + query + "\\y")
		: negated
			? match == null || !Regex.IsMatch(match, "\\y" + query + "\\y", RegexOptions.IgnoreCase)
			: match != null && Regex.IsMatch(match, "\\y" + query + "\\y", RegexOptions.IgnoreCase);
}
