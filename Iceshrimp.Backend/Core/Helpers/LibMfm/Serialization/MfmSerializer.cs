using System.Text;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;

public static class MfmSerializer {
	public static string Serialize(IEnumerable<MfmNode> nodes) {
		var result = new StringBuilder();

		foreach (var node in nodes) {
			switch (node) {
				case MfmCodeBlockNode mfmCodeBlockNode: {
					result.Append($"```{mfmCodeBlockNode.Language ?? ""}\n");
					result.Append(mfmCodeBlockNode.Code);
					result.Append("```");
					break;
				}
				case MfmMathBlockNode mfmMathBlockNode: {
					result.Append(@"\[");
					result.Append(mfmMathBlockNode.Formula);
					result.Append(@"\]");
					break;
				}
				case MfmSearchNode mfmSearchNode: {
					throw new NotImplementedException();
					break;
				}
				case MfmBoldNode mfmBoldNode: {
					result.Append("**");
					result.Append(Serialize(node.Children));
					result.Append("**");
					break;
				}
				case MfmCenterNode mfmCenterNode: {
					result.Append("<center>");
					result.Append(Serialize(node.Children));
					result.Append("</center>");
					break;
				}
				case MfmEmojiCodeNode mfmEmojiCodeNode: {
					result.Append($":{mfmEmojiCodeNode.Name}:");
					break;
				}
				case MfmFnNode mfmFnNode: {
					throw new NotImplementedException();
					break;
				}
				case MfmHashtagNode mfmHashtagNode: {
					result.Append($"#{mfmHashtagNode.Hashtag}");
					break;
				}
				case MfmInlineCodeNode mfmInlineCodeNode: {
					result.Append($"`{mfmInlineCodeNode.Code}`");
					break;
				}
				case MfmItalicNode mfmItalicNode: {
					result.Append("~~");
					result.Append(Serialize(node.Children));
					result.Append("~~");
					break;
				}
				case MfmLinkNode mfmLinkNode: {
					if (mfmLinkNode.Silent) result.Append('?');
					result.Append('[');
					result.Append(Serialize(node.Children));
					result.Append(']');
					result.Append($"({mfmLinkNode.Url})");
					break;
				}
				case MfmMathInlineNode mfmMathInlineNode: {
					result.Append(@"\(");
					result.Append(mfmMathInlineNode.Formula);
					result.Append(@"\)");
					break;
				}
				case MfmMentionNode mfmMentionNode: {
					result.Append($"@{mfmMentionNode.Username}");
					if (mfmMentionNode.Host != null)
						result.Append($"@{mfmMentionNode.Host}");
					break;
				}
				case MfmPlainNode: {
					result.Append(node.Children.OfType<MfmTextNode>().Select(p => p.Text));
					break;
				}
				case MfmSmallNode: {
					result.Append("<small>");
					result.Append(Serialize(node.Children));
					result.Append("</small>");
					break;
				}
				case MfmStrikeNode: {
					result.Append("~~");
					result.Append(Serialize(node.Children));
					result.Append("~~");
					break;
				}
				case MfmTextNode mfmTextNode: {
					result.Append(mfmTextNode.Text);
					break;
				}
				case MfmUnicodeEmojiNode mfmUnicodeEmojiNode: {
					result.Append(mfmUnicodeEmojiNode.Emoji);
					break;
				}
				case MfmUrlNode mfmUrlNode: {
					if (mfmUrlNode.Brackets)
						result.Append($"<{mfmUrlNode.Url}>");
					else
						result.Append(mfmUrlNode.Url);
					break;
				}
				case MfmQuoteNode mfmQuoteNode: {
					throw new NotImplementedException();
					break;
				}
				default: {
					throw new Exception("Unknown node type");
				}
			}
		}

		return result.ToString();
	}
}