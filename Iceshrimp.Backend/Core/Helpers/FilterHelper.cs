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

		foreach (var filter in filters)
		{
			var match = IsFiltered(note, filter);
			if (match != null) return (filter, match);
		}

		return null;
	}

	private static string? IsFiltered(Note note, Filter filter)
	{
		foreach (var keyword in filter.Keywords)
		{
			if (keyword.StartsWith('"') && keyword.EndsWith('"'))
			{
				var pattern = $@"\b{EfHelpers.EscapeRegexQuery(keyword[1..^1])}\b";
				var regex   = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));

				if (note.Text != null && regex.IsMatch(note.Text))
					return keyword;
				if (note.Cw != null && regex.IsMatch(note.Cw))
					return keyword;
			}
			else if ((note.Text != null && note.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)) ||
			         note.Cw != null && note.Cw.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
			{
				return keyword;
			}
		}

		return null;
	}
}