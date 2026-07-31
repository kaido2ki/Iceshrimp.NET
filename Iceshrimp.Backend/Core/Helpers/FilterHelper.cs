using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Helpers;

public static class FilterHelper
{
	public static (Filter filter, string keyword)? IsFiltered(IEnumerable<Note?> notes, List<Filter> filters)
	{
		if (filters.Count == 0) return null;

		foreach (var note in notes.OfType<Note>())
		{
			var match = IsFiltered(note, filters);
			if (match != null) return match;
		}

		return null;
	}

	private static (Filter filter, string keyword)? IsFiltered(Note note, List<Filter> filters)
	{
		if (filters.Count == 0) return null;
		if (note.Text == null && note.Cw == null) return null;

		foreach (var filter in filters.OrderBy(p => p.Action == Filter.FilterAction.Warn))
		{
			var match = IsFiltered(note, filter);
			if (match == null) continue;

			if (note.User != filter.User || filter.Action != Filter.FilterAction.Hide) return (filter, match);

			// If the note owner is the same as the filter owner and the action is hide we need to change the action to warn on a clone otherwise notes in the same batch may have the wrong action
			var f = filter.Clone(filter.User);
			f.Action = Filter.FilterAction.Warn;
			return (f, match);
		}

		return null;
	}

	public static List<(Filter filter, string keyword)> CheckFilters(IEnumerable<Note?> notes, List<Filter> filters)
	{
		if (filters.Count == 0) return [];

		var res = new List<(Filter filter, string keyword)>();

		foreach (var note in notes.OfType<Note>())
		{
			var match = CheckFilters(note, filters);
			if (match.Count != 0) res.AddRange(match);
		}

		return res;
	}

	private static List<(Filter filter, string keyword)> CheckFilters(Note note, List<Filter> filters)
	{
		if (filters.Count == 0) return [];

		var res = new List<(Filter filter, string keyword)>();

		foreach (var filter in filters)
		{
			var match = IsFiltered(note, filter);
			if (match != null) res.Add((filter, match));
		}

		return res;
	}

	private static string? IsFiltered(Note note, Filter filter)
	{
		foreach (var keyword in filter.Keywords)
		{
			if (keyword.StartsWith('"') && keyword.EndsWith('"'))
			{
				var pattern = $@"\b{EfHelpers.EscapeRegexQuery(keyword[1..^1])}\b";
				var regex   = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));

				try
				{
					if (note.Text != null && regex.IsMatch(note.Text))
						return keyword;
					if (note.Cw != null && regex.IsMatch(note.Cw))
						return keyword;
				}
				catch (RegexMatchTimeoutException)
				{
					return null;
				}
			}
			else if (keyword.StartsWith('/') && keyword.EndsWith('/'))
			{
				var regex = new Regex(keyword[1..^1], RegexOptions.IgnoreCase | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(0.75));

				try
				{
					if (note.Text != null && regex.IsMatch(note.Text))
						return keyword;
					if (note.Cw != null && regex.IsMatch(note.Cw))
						return keyword;
				}
				catch (RegexMatchTimeoutException)
				{
					return null;
				}
			}
			else if ((note.Text != null && note.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)) ||
			         (note.Cw != null && note.Cw.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)))
			{
				return keyword;
			}
		}

		return null;
	}
}