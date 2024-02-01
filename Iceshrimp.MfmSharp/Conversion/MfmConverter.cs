using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Iceshrimp.MfmSharp.Parsing;
using Iceshrimp.MfmSharp.Types;
using static Iceshrimp.MfmSharp.Parsing.HtmlParser;
using HtmlParser = AngleSharp.Html.Parser.HtmlParser;

namespace Iceshrimp.MfmSharp.Conversion;

public static class MfmConverter {
	public static async Task<string> FromHtmlAsync(string? html) {
		if (html == null) return "";

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		var dom = await new HtmlParser().ParseDocumentAsync(html);
		if (dom.Body == null) return "";

		var sb = new StringBuilder();
		dom.Body.ChildNodes.Select(ParseNode).ToList().ForEach(s => sb.Append(s));
		return sb.ToString().Trim();
	}

	public static async Task<string> ToHtmlAsync(string mfm) {
		var nodes = MfmParser.Parse(mfm);

		var context  = BrowsingContext.New();
		var document = await context.OpenNewAsync();
		var element  = document.CreateElement("p");

		foreach (var node in nodes) element.AppendNodes(document.FromMfmNode(node));

		await using var sw = new StringWriter();
		await element.ToHtmlAsync(sw);
		return sw.ToString();
	}

	private static INode FromMfmNode(this IDocument document, MfmNode node) {
		switch (node) {
			case MfmBoldNode: {
				var el = document.CreateElement("b");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmSmallNode: {
				var el = document.CreateElement("small");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmStrikeNode: {
				var el = document.CreateElement("del");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmItalicNode:
			case MfmFnNode: {
				var el = document.CreateElement("i");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmCodeBlockNode codeBlockNode: {
				var el    = document.CreateElement("pre");
				var inner = document.CreateElement("code");
				inner.TextContent = codeBlockNode.Code;
				el.AppendNodes(inner);
				return el;
			}
			case MfmCenterNode: {
				var el = document.CreateElement("div");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmEmojiCodeNode emojiCodeNode: {
				return document.CreateTextNode($"\u200B:{emojiCodeNode.Name}:\u200B");
			}
			case MfmUnicodeEmojiNode unicodeEmojiNode: {
				return document.CreateTextNode(unicodeEmojiNode.Emoji);
			}
			case MfmHashtagNode hashtagNode: {
				var el = document.CreateElement("a");
				//TODO: get url from config
				el.SetAttribute("href", $"https://example.org/tags/{hashtagNode.Hashtag}");
				el.TextContent = $"#{hashtagNode.Hashtag}";
				el.SetAttribute("rel", "tag");
				return el;
			}
			case MfmInlineCodeNode inlineCodeNode: {
				var el = document.CreateElement("code");
				el.TextContent = inlineCodeNode.Code;
				return el;
			}
			case MfmMathInlineNode mathInlineNode: {
				var el = document.CreateElement("code");
				el.TextContent = mathInlineNode.Formula;
				return el;
			}
			case MfmMathBlockNode mathBlockNode: {
				var el = document.CreateElement("code");
				el.TextContent = mathBlockNode.Formula;
				return el;
			}
			case MfmLinkNode linkNode: {
				var el = document.CreateElement("a");
				el.SetAttribute("href", linkNode.Url);
				el.AppendChildren(document, node);
				return el;
			}
			case MfmMentionNode mentionNode: {
				var el = document.CreateElement("span");
				el.TextContent = mentionNode.Acct;
				//TODO: Resolve mentions and only fall back to the above
				return el;
			}
			case MfmQuoteNode: {
				var el = document.CreateElement("blockquote");
				el.AppendChildren(document, node);
				return el;
			}
			case MfmTextNode textNode: {
				var el = document.CreateElement("span");
				var nodes = textNode.Text.Split("\r\n")
				                    .SelectMany(p => p.Split('\r'))
				                    .SelectMany(p => p.Split('\n'))
				                    .Select(document.CreateTextNode);

				foreach (var htmlNode in nodes) {
					el.AppendNodes(htmlNode);
					el.AppendNodes(document.CreateElement("br"));
				}

				if (el.LastChild != null)
					el.RemoveChild(el.LastChild);
				return el;
			}
			case MfmUrlNode urlNode: {
				var el = document.CreateElement("a");
				el.SetAttribute("href", urlNode.Url);
				var prefix = urlNode.Url.StartsWith("https://") ? "https://" : "http://";
				var length = prefix.Length;
				el.TextContent = urlNode.Url[length..];
				return el;
			}
			case MfmSearchNode searchNode: {
				//TODO: get search engine from config
				var el = document.CreateElement("a");
				el.SetAttribute("href", $"https://duckduckgo.com?q={HttpUtility.UrlEncode(searchNode.Query)}");
				el.TextContent = searchNode.Content;
				return el;
			}
			case MfmPlainNode: {
				var el = document.CreateElement("span");
				el.AppendChildren(document, node);
				return el;
			}
			default: {
				throw new NotImplementedException("Unsupported MfmNode type");
			}
		}
	}

	private static void AppendChildren(this INode element, IDocument document, MfmNode parent) {
		foreach (var node in parent.Children) element.AppendNodes(document.FromMfmNode(node));
	}
}