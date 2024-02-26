using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;

public static class MfmParser
{
	private static readonly ImmutableList<INodeParser> Parsers =
	[
		new PlainNodeParser(),
		new ItalicNodeParser(),
		new BoldNodeParser(),
		new SmallNodeParser(),
		new StrikeNodeParser(),
		new CenterNodeParser(),
		new HashtagNodeParser(),
		new MentionNodeParser(),
		new UrlNodeParser(),
		new AltUrlNodeParser(),
		new LinkNodeParser(),
		new SilentLinkNodeParser(),
		new InlineCodeNodeParser(),
		new EmojiCodeNodeParser(),
		new MathInlineNodeParser(),
		new MathBlockNodeParser(),
		new CodeBlockParser()
	];

	/// <remarks>
	///     This intentionally doesn't implement the node type UnicodeEmojiNode, both for performance and because it's not
	///     needed for backend processing
	/// </remarks>
	public static IEnumerable<MfmNode> Parse(string buffer, int position = 0, int nestLimit = 20)
	{
		if (nestLimit <= 0) return [];
		var nodes = new List<MfmNode>();
		while (position < buffer.Length)
		{
			var parser = Parsers.FirstOrDefault(p => p.IsValid(buffer, position));
			if (parser == null)
			{
				if (nodes.LastOrDefault() is MfmTextNode textNode)
				{
					textNode.Text += buffer[position++];
				}
				else
				{
					var node = new MfmTextNode { Text = buffer[position++].ToString() };

					nodes.Add(node);
				}

				continue;
			}

			var result = parser.Parse(buffer, position, nestLimit);
			position += result.chars;
			nodes.Add(result.node);
		}

		return nodes;
	}
}

internal static class NodeParserAbstractions
{
	public static (int start, int end, int chars) HandlePosition(string pre, string post, string buffer, int position)
	{
		var start = position + pre.Length;
		//TODO: cover case of buffer == string.empty
		var end = buffer.IndexOf(post, start, StringComparison.Ordinal);
		int chars;
		if (end == -1)
		{
			end   = buffer.Length;
			chars = end - position;
		}
		else
		{
			chars = end - position + post.Length;
		}

		return (start, end, chars);
	}

	public static (int start, int end, int chars) HandlePosition(string character, string buffer, int position)
	{
		return HandlePosition(character, character, buffer, position);
	}

	public static (int start, int end, int chars) HandlePosition(string pre, Regex regex, string buffer, int position)
	{
		var start = position + pre.Length;
		var end   = regex.Match(buffer[start..]).Index + start;
		var chars = end - position;

		return (start, end, chars);
	}
}

internal interface INodeParser
{
	public bool IsValid(string buffer, int position);

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit);
}

internal class ItalicNodeParser : INodeParser
{
	private const string Char = "*";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Char) && !buffer[position..].StartsWith("**");
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);

		var node = new MfmItalicNode
		{
			Children = MfmParser.Parse(buffer[start..end], 0, --nestLimit).OfType<MfmInlineNode>()
		};

		return (node, chars);
	}
}

internal class InlineCodeNodeParser : INodeParser
{
	private const string Char = "`";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Char) && !buffer[position..].StartsWith("```");
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);

		var node = new MfmInlineCodeNode { Code = buffer[start..end] };

		return (node, chars);
	}
}

internal class BoldNodeParser : INodeParser
{
	private const string Char = "**";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Char);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);

		var node = new MfmBoldNode
		{
			Children = MfmParser.Parse(buffer[start..end], 0, --nestLimit).OfType<MfmInlineNode>()
		};

		return (node, chars);
	}
}

internal class PlainNodeParser : INodeParser
{
	private const string Pre  = "<plain>";
	private const string Post = "</plain>";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmPlainNode { Children = [new MfmTextNode { Text = buffer[start..end] }] };

		return (node, chars);
	}
}

internal class SmallNodeParser : INodeParser
{
	private const string Pre  = "<small>";
	private const string Post = "</small>";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmSmallNode
		{
			Children = MfmParser.Parse(buffer[start..end], 0, --nestLimit).OfType<MfmInlineNode>()
		};

		return (node, chars);
	}
}

internal class CenterNodeParser : INodeParser
{
	private const string Pre  = "<center>";
	private const string Post = "</center>";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmCenterNode
		{
			Children = MfmParser.Parse(buffer[start..end], 0, --nestLimit).OfType<MfmInlineNode>()
		};

		return (node, chars);
	}
}

internal class StrikeNodeParser : INodeParser
{
	private const string Char = "~~";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Char);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);

		var node = new MfmStrikeNode
		{
			Children = MfmParser.Parse(buffer[start..end], 0, --nestLimit).OfType<MfmInlineNode>()
		};

		return (node, chars);
	}
}

internal class HashtagNodeParser : INodeParser
{
	private const           string Pre  = "#";
	private static readonly Regex  Post = new(@"\s|$");

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmHashtagNode { Hashtag = buffer[start..end] };

		return (node, chars);
	}
}

internal class MentionNodeParser : INodeParser
{
	private const           string Pre        = "@";
	private static readonly Regex  Post       = new(@"[\s\),']|:(?:[^a-zA-Z]|)|$");
	private static readonly Regex  Full       = new(@"^[a-zA-Z0-9._\-]+(?:@[a-zA-Z0-9._\-]+\.[a-zA-Z0-9._\-]+)?$");
	private static readonly Regex  Lookbehind = new(@"\s");

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Pre)) return false;
		if (position != 0 && !Lookbehind.IsMatch(buffer[position - 1].ToString())) return false;

		var (start, end, _) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		return buffer[start..end].Split("@").Length <= 2 && Full.IsMatch(buffer[start..end]);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		//TODO: make sure this handles non-ascii/puny domains
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		var split = buffer[start..end].Split("@");

		var node = new MfmMentionNode
		{
			Username = split[0], Host = split.Length == 2 ? split[1] : null, Acct = $"@{buffer[start..end]}"
		};

		return (node, chars);
	}
}

internal class UrlNodeParser : INodeParser
{
	private const string Pre    = "https://";
	private const string PreAlt = "http://";

	private static readonly Regex Post = new(@"\s|$");

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Pre) && !buffer[position..].StartsWith(PreAlt))
			return false;

		var prefix = buffer[position..].StartsWith(Pre) ? Pre : PreAlt;
		var (start, end, _) = NodeParserAbstractions.HandlePosition(prefix, Post, buffer, position);
		var result = Uri.TryCreate(prefix + buffer[start..end], UriKind.Absolute, out var uri);
		return result && uri?.Scheme is "http" or "https";
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var prefix = buffer[position..].StartsWith(Pre) ? Pre : PreAlt;
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(prefix, Post, buffer, position);

		var node = new MfmUrlNode { Url = prefix + buffer[start..end], Brackets = false };

		return (node, chars);
	}
}

internal class AltUrlNodeParser : INodeParser
{
	private const string Pre    = "<https://";
	private const string PreAlt = "<http://";
	private const string Post   = ">";

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Pre) && !buffer[position..].StartsWith(PreAlt))
			return false;

		var prefix = buffer[position..].StartsWith(Pre) ? Pre : PreAlt;
		var (start, end, _) = NodeParserAbstractions.HandlePosition(prefix, Post, buffer, position);
		var result = Uri.TryCreate(prefix[1..] + buffer[start..end], UriKind.Absolute, out var uri);
		return result && uri?.Scheme is "http" or "https";
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var prefix = buffer[position..].StartsWith(Pre) ? Pre : PreAlt;
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(prefix, Post, buffer, position);

		var node = new MfmUrlNode { Url = prefix[1..] + buffer[start..end], Brackets = true };

		return (node, chars);
	}
}

internal class LinkNodeParser : INodeParser
{
	private const           string Pre  = "[";
	private const           string Post = ")";
	private static readonly Regex  Full = new(@"^\[(.+?)\]\((.+?)\)$");

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Pre))
			return false;

		var (_, end, _) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		if (end == buffer.Length)
			return false;

		var match = Full.Match(buffer[position..(end + 1)]);
		if (match.Groups.Count != 3)
			return false;

		var result = Uri.TryCreate(match.Groups[2].Value, UriKind.Absolute, out var uri);
		return result && uri?.Scheme is "http" or "https";
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		var textEnd = buffer[position..].IndexOf(']') + position;

		var match = Full.Match(buffer[position..(end + 1)]);

		var node = new MfmLinkNode
		{
			Url      = match.Groups[2].Value,
			Children = MfmParser.Parse(buffer[start..textEnd], 0, --nestLimit).OfType<MfmInlineNode>(),
			Silent   = false
		};

		return (node, chars);
	}
}

internal class SilentLinkNodeParser : INodeParser
{
	private const           string Pre  = "?[";
	private const           string Post = ")";
	private static readonly Regex  Full = new(@"^\?\[(.+?)\]\((.+?)\)$");

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Pre))
			return false;

		var (_, end, _) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		if (end == buffer.Length)
			return false;

		var match = Full.Match(buffer[position..(end + 1)]);
		if (match.Groups.Count != 3)
			return false;

		var result = Uri.TryCreate(match.Groups[2].Value, UriKind.Absolute, out var uri);
		return result && uri?.Scheme is "http" or "https";
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);
		var textEnd = buffer[position..].IndexOf(']') + position;

		var match = Full.Match(buffer[position..(end + 1)]);

		var node = new MfmLinkNode
		{
			Url      = match.Groups[2].Value,
			Children = MfmParser.Parse(buffer[start..textEnd], 0, --nestLimit).OfType<MfmInlineNode>(),
			Silent   = true
		};

		return (node, chars);
	}
}

internal class EmojiCodeNodeParser : INodeParser
{
	private const           string Char = ":";
	private static readonly Regex  Full = new("^[a-z0-9_+-]+$");

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Char))
			return false;

		var (start, end, _) = NodeParserAbstractions.HandlePosition(Char, buffer, position);
		return end != buffer.Length && Full.IsMatch(buffer[start..end]);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);

		var node = new MfmEmojiCodeNode { Name = buffer[start..end] };

		return (node, chars);
	}
}

internal class MathInlineNodeParser : INodeParser
{
	private const string Pre  = @"\(";
	private const string Post = @"\)";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmMathInlineNode { Formula = buffer[start..end] };

		return (node, chars);
	}
}

internal class MathBlockNodeParser : INodeParser
{
	private const string Pre  = @"\[";
	private const string Post = @"\]";

	public bool IsValid(string buffer, int position)
	{
		return buffer[position..].StartsWith(Pre);
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Pre, Post, buffer, position);

		var node = new MfmMathBlockNode { Formula = buffer[start..end] };

		return (node, chars);
	}
}

internal class CodeBlockParser : INodeParser
{
	private const string Char = "```";

	public bool IsValid(string buffer, int position)
	{
		if (!buffer[position..].StartsWith(Char)) return false;

		var (start, end, _) = NodeParserAbstractions.HandlePosition(Char, buffer, position);
		return buffer[start..end].EndsWith('\n');
	}

	public (MfmNode node, int chars) Parse(string buffer, int position, int nestLimit)
	{
		var (start, end, chars) = NodeParserAbstractions.HandlePosition(Char, buffer, position);
		var split = buffer[start..end].Split('\n');
		var lang  = split[0].Length > 0 ? split[0] : null;
		var code  = string.Join('\n', split[1..^1]);

		var node = new MfmCodeBlockNode { Code = code, Language = lang };

		return (node, chars);
	}
}

//TODO: still missing: FnNode, MfmSearchNode, MfmQuoteNode
//TODO: "*italic **bold** *" doesn't work yet