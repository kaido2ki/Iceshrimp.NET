namespace Iceshrimp.Parsing;

public static class SearchQueryParser
{
	public static List<ISearchQueryFilter> Parse(ReadOnlySpan<char> input)
	{
		var results = new List<ISearchQueryFilter>();

		input = input.Trim();
		if (input.Length == 0) return [];

		int pos = 0;
		while (pos < input.Length)
		{
			var oldPos = pos;
			var res    = ParseToken(input, ref pos);
			if (res == null) return results;
			if (pos <= oldPos) throw new Exception("Infinite loop detected!");
			results.Add(res);
		}

		return results;
	}

	private static ISearchQueryFilter? ParseToken(ReadOnlySpan<char> input, ref int pos)
	{
		while (input[pos] == ' ')
		{
			pos++;
			if (pos >= input.Length) return null;
		}

		var negated = false;
		if (input[pos] == '-' && input.Length > pos + 1)
		{
			negated = true;
			pos++;
		}

		if (input[pos] == '"' && input.Length > pos + 2)
		{
			var closingQuote = pos + 1 + input[(pos + 1)..].IndexOf('"');
			if (closingQuote != -1)
			{
				var literalRes = new WordFilter(negated, input[++pos..closingQuote].ToString());
				pos = closingQuote + 1;
				return literalRes;
			}
		}

		if (input[pos] == '(' && input.Length > pos + 2)
		{
			var closingParen = pos + 1 + input[(pos + 1)..].IndexOf(')');
			if (closingParen != -1)
			{
				var items      = input[++pos..closingParen].ToString().Split(" OR ").Select(p => p.Trim()).ToArray();
				var literalRes = new MultiWordFilter(negated, items);
				if (items.Length > 0)
				{
					pos = closingParen + 1;
					return literalRes;
				}
			}
		}

		var end = input[pos..].IndexOf(' ');
		if (end == -1)
			end = input.Length;
		else
			end += pos;

		var splitIdx = input[pos..end].IndexOf(':');
		var keyRange = splitIdx < 1 ? ..0 : pos..(pos + splitIdx);
		var key      = splitIdx < 1 ? ReadOnlySpan<char>.Empty : input[keyRange];
		var value    = splitIdx < 1 ? input : input[(keyRange.End.Value + 1)..end];

		ISearchQueryFilter res = key switch
		{
			"cw"                                    => new CwFilter(negated, value.ToString()),
			"from" or "author" or "by" or "user"    => new FromFilter(negated, value.ToString()),
			"mention" or "mentions" or "mentioning" => new MentionFilter(negated, value.ToString()),
			"reply" or "replying" or "to"           => new ReplyFilter(negated, value.ToString()),
			"instance" or "domain" or "host"        => new InstanceFilter(negated, value.ToString()),

			"filter" when MiscFilter.TryParse(negated, value, out var parsed) => parsed,
			"in" when InFilter.TryParse(negated, value, out var parsed)       => parsed,
			"has" or "attachment" or "attached" when AttachmentFilter.TryParse(negated, value, out var parsed)
				=> parsed,

			"case" when CaseFilter.TryParse(value, out var parsed)   => parsed,
			"match" when MatchFilter.TryParse(value, out var parsed) => parsed,

			"after" or "since" when DateOnly.TryParse(value, out var date)  => new AfterFilter(date),
			"before" or "until" when DateOnly.TryParse(value, out var date) => new BeforeFilter(date),

			_ => new WordFilter(negated, input[pos..end].ToString())
		};

		pos = end;
		return res;
	}
}
