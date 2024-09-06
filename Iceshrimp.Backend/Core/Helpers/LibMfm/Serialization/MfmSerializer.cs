using System.Text;
using static Iceshrimp.Parsing.MfmNodeTypes;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;

public static class MfmSerializer
{
	public static string Serialize(IEnumerable<MfmNode> nodes)
	{
		var result = new StringBuilder();

		foreach (var node in nodes)
		{
			switch (node)
			{
				case MfmCodeBlockNode mfmCodeBlockNode:
				{
					result.Append($"```{mfmCodeBlockNode.Language?.Value ?? ""}\n");
					result.Append(mfmCodeBlockNode.Code);
					result.Append("\n```");
					break;
				}
				case MfmMathBlockNode mfmMathBlockNode:
				{
					result.Append(@"\[");
					result.Append(mfmMathBlockNode.Formula);
					result.Append(@"\]");
					break;
				}
				case MfmSearchNode mfmSearchNode:
				{
					result.Append(mfmSearchNode.Query);
					result.Append(" [search]");
					break;
				}
				case MfmBoldNode:
				{
					result.Append("**");
					result.Append(Serialize(node.Children));
					result.Append("**");
					break;
				}
				case MfmCenterNode:
				{
					result.Append("<center>");
					result.Append(Serialize(node.Children));
					result.Append("</center>");
					break;
				}
				case MfmEmojiCodeNode mfmEmojiCodeNode:
				{
					result.Append($":{mfmEmojiCodeNode.Name}:");
					break;
				}
				case MfmFnNode mfmFnNode:
				{
					result.Append("$[");
					result.Append(mfmFnNode.Name);
					result.Append('.');
					var args = mfmFnNode.Args.Select(p => p.Value != null ? $"{p.Key}={p.Value}" : $"{p.Key}");
					result.Append(string.Join(',', args));
					result.Append(' ');
					result.Append(Serialize(node.Children));
					result.Append(']');
					break;
				}
				case MfmHashtagNode mfmHashtagNode:
				{
					result.Append($"#{mfmHashtagNode.Hashtag}");
					break;
				}
				case MfmInlineCodeNode mfmInlineCodeNode:
				{
					result.Append($"`{mfmInlineCodeNode.Code}`");
					break;
				}
				case MfmItalicNode:
				{
					result.Append('*');
					result.Append(Serialize(node.Children));
					result.Append('*');
					break;
				}
				case MfmLinkNode mfmLinkNode:
				{
					if (mfmLinkNode.Silent) result.Append('?');
					result.Append('[');
					result.Append(Serialize(node.Children));
					result.Append(']');
					result.Append($"({mfmLinkNode.Url})");
					break;
				}
				case MfmMathInlineNode mfmMathInlineNode:
				{
					result.Append(@"\(");
					result.Append(mfmMathInlineNode.Formula);
					result.Append(@"\)");
					break;
				}
				case MfmMentionNode mfmMentionNode:
				{
					result.Append($"@{mfmMentionNode.Username}");
					if (mfmMentionNode.Host != null)
						result.Append($"@{mfmMentionNode.Host.Value}");
					break;
				}
				case MfmPlainNode:
				{
					result.Append("<plain>");
					foreach (var s in node.Children.OfType<MfmTextNode>().Select(p => p.Text))
						result.Append(s);
					result.Append("</plain>");
					break;
				}
				case MfmSmallNode:
				{
					result.Append("<small>");
					result.Append(Serialize(node.Children));
					result.Append("</small>");
					break;
				}
				case MfmStrikeNode:
				{
					result.Append("~~");
					result.Append(Serialize(node.Children));
					result.Append("~~");
					break;
				}
				case MfmTextNode mfmTextNode:
				{
					result.Append(mfmTextNode.Text);
					break;
				}
				case MfmUrlNode mfmUrlNode:
				{
					if (mfmUrlNode.Brackets)
						result.Append($"<{mfmUrlNode.Url}>");
					else
						result.Append(mfmUrlNode.Url);
					break;
				}
				case MfmQuoteNode mfmQuoteNode:
				{
					var serialized = Serialize(node.Children);
					var split      = serialized.Split('\n');

					for (var i = 0; i < split.Length; i++)
					{
						split[i] = "> " + split[i];
					}

					result.Append(string.Join('\n', split));
					if (!mfmQuoteNode.FollowedByEof)
						result.Append('\n');
					if (mfmQuoteNode.FollowedByQuote)
						result.Append('\n');
					break;
				}
				default:
				{
					throw new Exception("Unknown node type");
				}
			}
		}

		return result.ToString();
	}
}