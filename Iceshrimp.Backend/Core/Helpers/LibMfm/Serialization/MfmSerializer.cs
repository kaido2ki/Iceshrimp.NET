using System.Text;
using static Iceshrimp.Parsing.MfmNodeTypes;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;

public static class MfmSerializer
{
	public static string Serialize(IEnumerable<MfmNode> nodes) => SerializeInternal(nodes).Trim();

	private static string SerializeInternal(IEnumerable<MfmNode> nodes)
	{
		var result = new StringBuilder();

		foreach (var node in nodes)
		{
			switch (node)
			{
				case MfmCodeBlockNode mfmCodeBlockNode:
				{
					result.Append($"\n```{mfmCodeBlockNode.Language?.Value ?? ""}\n");
					result.Append(mfmCodeBlockNode.Code);
					result.Append("\n```\n");
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
				case MfmBoldNode mfmBoldNode:
				{
					var start = mfmBoldNode.Type.IsSymbol ? "**" : "<b>";
					var end   = mfmBoldNode.Type.IsSymbol ? "**" : "</b>";
					result.Append(start);
					result.Append(SerializeInternal(node.Children));
					result.Append(end);
					break;
				}
				case MfmCenterNode:
				{
					result.Append("<center>");
					result.Append(SerializeInternal(node.Children));
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
					if (mfmFnNode.Args is { } args)
					{
						result.Append('.');
						var str = args.Value.Select(p => p.Value != null ? $"{p.Key}={p.Value.Value}" : $"{p.Key}");
						result.Append(string.Join(',', str));
					}

					result.Append(' ');
					result.Append(SerializeInternal(node.Children));
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
				case MfmItalicNode mfmItalicNode:
				{
					var start = mfmItalicNode.Type.IsSymbol ? "*" : "<i>";
					var end   = mfmItalicNode.Type.IsSymbol ? "*" : "</i>";
					result.Append(start);
					result.Append(SerializeInternal(node.Children));
					result.Append(end);
					break;
				}
				case MfmLinkNode mfmLinkNode:
				{
					if (mfmLinkNode.Silent) result.Append('?');
					result.Append('[');
					result.Append(SerializeInternal(node.Children));
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
					result.Append(SerializeInternal(node.Children));
					result.Append("</small>");
					break;
				}
				case MfmStrikeNode mfmStrikeNode:
				{
					var start = mfmStrikeNode.Type.IsSymbol ? "~~" : "<s>";
					var end   = mfmStrikeNode.Type.IsSymbol ? "~~" : "</s>";
					result.Append(start);
					result.Append(SerializeInternal(node.Children));
					result.Append(end);
					break;
				}
				case MfmTimeoutTextNode mfmTimeoutTextNode:
				{
					// This mitigates MFM DoS payloads, since every incoming note is parsed & reserialized.
					// We need to remove all </plain> tags to make sure that the mitigation can't be bypassed by adding </plain> to the payload.
					// Opening <plain> tags are removed because they are now unnecessary.
					result.Append("<plain>");
					result.Append(mfmTimeoutTextNode.Text.Replace("<plain>", "").Replace("</plain>", ""));
					result.Append("</plain>");
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
					var serialized = SerializeInternal(node.Children);
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