using AngleSharp.Dom;

namespace Iceshrimp.MfmSharp.Parsing;

internal static class HtmlParser {
	internal static string? ParseNode(INode node) {
		if (node.NodeType is NodeType.Text)
			return node.TextContent;
		if (node.NodeType is NodeType.Comment or NodeType.Document)
			return null;

		switch (node.NodeName) {
			case "BR": {
				return "\n";
			}
			case "A": {
				//TODO: implement parsing of links & mentions (automatically correct split domain mentions for the latter)
				return null;
			}
			case "H1": {
				return $"【{ParseChildren(node)}】\n";
			}
			case "B":
			case "STRONG": {
				return $"**{ParseChildren(node)}**";
			}
			case "SMALL": {
				return $"<small>{ParseChildren(node)}</small>";
			}
			case "S":
			case "DEL": {
				return $"~~{ParseChildren(node)}~~";
			}
			case "I":
			case "EM": {
				return $"<i>{ParseChildren(node)}</i>";
			}
			case "PRE": {
				return node.ChildNodes is [{ NodeName: "CODE" }]
					? $"\n```\n{string.Join(null, node.ChildNodes[0].TextContent)}\n```\n"
					: ParseChildren(node);
			}
			case "CODE": {
				return $"`{ParseChildren(node)}`";
			}
			case "BLOCKQUOTE": {
				return node.TextContent.Length > 0
					? $"\n> {string.Join("\n> ", node.TextContent.Split("\n"))}"
					: null;
			}

			case "P":
			case "H2":
			case "H3":
			case "H4":
			case "H5":
			case "H6": {
				return $"\n\n{ParseChildren(node)}";
			}

			case "DIV":
			case "HEADER":
			case "FOOTER":
			case "ARTICLE":
			case "LI":
			case "DT":
			case "DD": {
				return $"\n{ParseChildren(node)}";
			}

			default: {
				return ParseChildren(node);
			}
		}
	}

	private static string ParseChildren(INode node) {
		return string.Join(null, node.ChildNodes.Select(ParseNode));
	}
}