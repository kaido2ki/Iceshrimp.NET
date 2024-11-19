using AngleSharp;
using AngleSharp.Dom;
using Iceshrimp.Parsing;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public static class MfmRenderer
{
	public static async Task<MarkupString> RenderStringAsync(
		string text, List<EmojiResponse> emoji, bool simple = false
	)
	{
		var res         = simple ? Mfm.parseSimple(text) : Mfm.parse(text);
		var context     = BrowsingContext.New();
		var document    = await context.OpenNewAsync();
		var renderedMfm = RenderMultipleNodes(res, document, emoji, simple);
		var html        = renderedMfm.ToHtml();
		return new MarkupString(html);
	}

	private static INode RenderMultipleNodes(
		IEnumerable<MfmNodeTypes.MfmNode> nodes, IDocument document, List<EmojiResponse> emoji, bool simple
	)
	{
		var el = document.CreateElement("span");
		el.SetAttribute("mfm", "mfm");
		el.ClassName = "mfm";
		foreach (var node in nodes)
		{
			try
			{
				el.AppendNodes(RenderNode(node, document, emoji, simple));
			}
			catch (NotImplementedException e)
			{
				var fallback = document.CreateElement("span");
				fallback.TextContent = $"[Node type <{e.Message}> not implemented]";
				el.AppendNodes(fallback);
			}
		}

		return el;
	}

	private static INode RenderNode(
		MfmNodeTypes.MfmNode node, IDocument document, List<EmojiResponse> emoji, bool simple
	)
	{
		// Hard wrap makes this impossible to read
		// @formatter:off
		var rendered = node switch
		{
			MfmNodeTypes.MfmCenterNode mfmCenterNode         => MfmCenterNode(mfmCenterNode, document),
			MfmNodeTypes.MfmCodeBlockNode mfmCodeBlockNode   => MfmCodeBlockNode(mfmCodeBlockNode, document),
			MfmNodeTypes.MfmMathBlockNode mfmMathBlockNode   => throw new NotImplementedException($"{mfmMathBlockNode.GetType()}"),
			MfmNodeTypes.MfmQuoteNode mfmQuoteNode           => MfmQuoteNode(mfmQuoteNode, document),
			MfmNodeTypes.MfmSearchNode mfmSearchNode         => throw new NotImplementedException($"{mfmSearchNode.GetType()}"),
			MfmNodeTypes.MfmBlockNode mfmBlockNode           => throw new NotImplementedException($"{mfmBlockNode.GetType()}"),
			MfmNodeTypes.MfmBoldNode mfmBoldNode             => MfmBoldNode(mfmBoldNode, document),
			MfmNodeTypes.MfmEmojiCodeNode mfmEmojiCodeNode   => MfmEmojiCodeNode(mfmEmojiCodeNode, document, emoji, simple),
			MfmNodeTypes.MfmFnNode mfmFnNode                 => throw new NotImplementedException($"{mfmFnNode.GetType()}"),
			MfmNodeTypes.MfmHashtagNode mfmHashtagNode       => MfmHashtagNode(mfmHashtagNode, document),
			MfmNodeTypes.MfmInlineCodeNode mfmInlineCodeNode => MfmInlineCodeNode(mfmInlineCodeNode, document),
			MfmNodeTypes.MfmItalicNode mfmItalicNode         => MfmItalicNode(mfmItalicNode, document),
			MfmNodeTypes.MfmLinkNode mfmLinkNode             => MfmLinkNode(mfmLinkNode, document),
			MfmNodeTypes.MfmMathInlineNode mfmMathInlineNode => throw new NotImplementedException($"{mfmMathInlineNode.GetType()}"),
			MfmNodeTypes.MfmMentionNode mfmMentionNode       => MfmMentionNode(mfmMentionNode, document),
			MfmNodeTypes.MfmPlainNode mfmPlainNode           => MfmPlainNode(mfmPlainNode, document),
			MfmNodeTypes.MfmSmallNode mfmSmallNode           => MfmSmallNode(mfmSmallNode, document),
			MfmNodeTypes.MfmStrikeNode mfmStrikeNode         => MfmStrikeNode(mfmStrikeNode, document),
			MfmNodeTypes.MfmTextNode mfmTextNode             => MfmTextNode(mfmTextNode, document),
			MfmNodeTypes.MfmUrlNode mfmUrlNode               => MfmUrlNode(mfmUrlNode, document),
			MfmNodeTypes.MfmInlineNode mfmInlineNode         => throw new NotImplementedException($"{mfmInlineNode.GetType()}"),
			_ => throw new ArgumentOutOfRangeException(nameof(node))
		};
		// @formatter:on

		if (node.Children.Length > 0)
		{
			foreach (var childNode in node.Children)
			{
				try
				{
					rendered.AppendNodes(RenderNode(childNode, document, emoji, simple));
				}
				catch (NotImplementedException e)
				{
					var fallback = document.CreateElement("span");
					fallback.TextContent = $"[Node type <{e.Message}> not implemented]";
					rendered.AppendNodes(fallback);
				}
			}
		}

		return rendered;
	}

	private static INode MfmPlainNode(MfmNodeTypes.MfmPlainNode _, IDocument document)
	{
		var el = document.CreateElement("span");
		el.ClassName = "plain";
		return el;
	}

	private static INode MfmCenterNode(MfmNodeTypes.MfmCenterNode _, IDocument document)
	{
		var el = document.CreateElement("div");
		el.SetAttribute("style", "text-align: center");
		return el;
	}

	private static INode MfmCodeBlockNode(MfmNodeTypes.MfmCodeBlockNode node, IDocument document)
	{
		var el = document.CreateElement("pre");
		el.ClassName = "code-pre";
		var childEl = document.CreateElement("code");
		childEl.TextContent = node.Code;
		el.AppendChild(childEl);
		return el;
	}

	private static INode MfmQuoteNode(MfmNodeTypes.MfmQuoteNode _, IDocument document)
	{
		var el = document.CreateElement("blockquote");
		el.ClassName = "quote-node";
		return el;
	}

	private static INode MfmInlineCodeNode(MfmNodeTypes.MfmInlineCodeNode node, IDocument document)
	{
		var el = document.CreateElement("code");
		el.TextContent = node.Code;
		return el;
	}

	private static INode MfmHashtagNode(MfmNodeTypes.MfmHashtagNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", $"/tags/{node.Hashtag}");
		el.ClassName   = "hashtag-node";
		el.TextContent = "#" + node.Hashtag;
		return el;
	}

	private static INode MfmLinkNode(MfmNodeTypes.MfmLinkNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", node.Url);
		el.SetAttribute("target", "_blank");
		el.ClassName = "link-node";
		return el;
	}

	private static INode MfmItalicNode(MfmNodeTypes.MfmItalicNode _, IDocument document)
	{
		var el = document.CreateElement("span");
		el.SetAttribute("style", "font-style: italic");
		return el;
	}

	private static INode MfmEmojiCodeNode(
		MfmNodeTypes.MfmEmojiCodeNode node, IDocument document, List<EmojiResponse> emojiList, bool simple
	)
	{
		var el = document.CreateElement("span");
		el.ClassName = simple ? "emoji simple" : "emoji";

		var emoji = emojiList.Find(p => p.Name == node.Name);
		if (emoji is null)
		{
			el.TextContent = node.Name;
		}
		else
		{
			var image = document.CreateElement("img");
			image.SetAttribute("src", emoji.PublicUrl);
			image.SetAttribute("alt", node.Name);
			image.SetAttribute("title", $":{emoji.Name}:");
			el.AppendChild(image);
		}

		return el;
	}

	private static INode MfmUrlNode(MfmNodeTypes.MfmUrlNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", node.Url);
		el.SetAttribute("target", "_blank");
		el.ClassName   = "url-node";
		el.TextContent = node.Url;
		return el;
	}

	private static INode MfmBoldNode(MfmNodeTypes.MfmBoldNode _, IDocument document)
	{
		var el = document.CreateElement("strong");
		return el;
	}

	private static INode MfmSmallNode(MfmNodeTypes.MfmSmallNode _, IDocument document)
	{
		var el = document.CreateElement("small");
		el.SetAttribute("style", "opacity: 0.7;");
		return el;
	}

	private static INode MfmStrikeNode(MfmNodeTypes.MfmStrikeNode _, IDocument document)
	{
		var el = document.CreateElement("del");
		return el;
	}

	private static INode MfmTextNode(MfmNodeTypes.MfmTextNode node, IDocument document)
	{
		var el = document.CreateElement("span");
		el.TextContent = node.Text;
		return el;
	}

	private static INode MfmMentionNode(MfmNodeTypes.MfmMentionNode node, IDocument document)
	{
		var link = document.CreateElement("a");
		link.SetAttribute("href", $"/@{node.Acct}");
		link.ClassName = "mention";
		var userPart = document.CreateElement("span");
		userPart.ClassName   = "user";
		userPart.TextContent = $"@{node.Username}";
		link.AppendChild(userPart);
		if (node.Host != null)
		{
			var hostPart = document.CreateElement("span");
			hostPart.ClassName   = "host";
			hostPart.TextContent = $"@{node.Host.Value}";
			link.AppendChild(hostPart);
		}

		return link;
	}
}