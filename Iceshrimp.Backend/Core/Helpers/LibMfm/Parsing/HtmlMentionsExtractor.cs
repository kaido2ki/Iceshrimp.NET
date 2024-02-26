using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;

internal class HtmlMentionsExtractor
{
	internal List<string> Mentions { get; } = [];

	private void ParseNode(INode node)
	{
		if (node.NodeType is NodeType.Text)
			return;
		if (node.NodeType is NodeType.Comment or NodeType.Document)
			return;

		switch (node.NodeName)
		{
			case "A":
			{
				if (node is not HtmlElement el) return;
				var href = el.GetAttribute("href");
				if (href == null) return;
				if (el.ClassList.Contains("u-url") && el.ClassList.Contains("mention"))
					Mentions.Add(href);
				return;
			}
			case "PRE":
			{
				if (node.ChildNodes is [{ NodeName: "CODE" }])
					return;
				ParseChildren(node);
				return;
			}
			case "BR":
			case "BLOCKQUOTE":
			{
				return;
			}

			default:
			{
				ParseChildren(node);
				return;
			}
		}
	}

	internal void ParseChildren(INode node)
	{
		foreach (var child in node.ChildNodes) ParseNode(child);
	}
}