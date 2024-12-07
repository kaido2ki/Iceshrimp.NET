using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.MfmSharp;
using Microsoft.Extensions.Options;
using MfmHtmlParser = Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing.HtmlParser;
using HtmlParser = AngleSharp.Html.Parser.HtmlParser;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;

public class MfmConverter(
	IOptions<Config.InstanceSection> config
) : ISingletonService
{
	public AsyncLocal<bool> SupportsHtmlFormatting { get; } = new();

	public static async Task<string?> FromHtmlAsync(string? html, List<Note.MentionedUser>? mentions = null)
	{
		if (html == null) return null;

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		// Ensure compatibility with AP servers that send non-breaking space characters instead of regular spaces
		html = html.Replace("\u00A0", " ");

		var dom = await new HtmlParser().ParseDocumentAsync(html);
		if (dom.Body == null) return "";

		var sb     = new StringBuilder();
		var parser = new MfmHtmlParser(mentions ?? []);
		dom.Body.ChildNodes.Select(parser.ParseNode).ToList().ForEach(s => sb.Append(s));
		return sb.ToString().Trim();
	}

	public static async Task<List<string>> ExtractMentionsFromHtmlAsync(string? html)
	{
		if (html == null) return [];

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		var dom = await new HtmlParser().ParseDocumentAsync(html);
		if (dom.Body == null) return [];

		var parser = new HtmlMentionsExtractor();
		foreach (var node in dom.Body.ChildNodes)
			parser.ParseChildren(node);

		return parser.Mentions;
	}

	public async Task<string> ToHtmlAsync(
		IEnumerable<IMfmNode> nodes, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "p",
		List<Emoji>? emoji = null
	)
	{
		var context    = BrowsingContext.New();
		var document   = await context.OpenNewAsync();
		var element    = document.CreateElement(rootElement);
		var nodeList   = nodes.ToList();
		var hasContent = nodeList.Count > 0;

		if (replyInaccessible)
		{
			var wrapper = document.CreateElement("span");
			var re      = document.CreateElement("span");
			re.TextContent = "RE: \ud83d\udd12"; // lock emoji
			wrapper.AppendChild(re);

			if (hasContent)
			{
				wrapper.AppendChild(document.CreateElement("br"));
				wrapper.AppendChild(document.CreateElement("br"));
			}

			element.AppendChild(wrapper);
		}

		foreach (var node in nodeList) element.AppendNodes(FromMfmNode(document, node, mentions, host, emoji));

		if (quoteUri != null)
		{
			var a = document.CreateElement("a");
			a.SetAttribute("href", quoteUri);
			a.TextContent = quoteUri.StartsWith("https://") ? quoteUri[8..] : quoteUri[7..];
			var quote = document.CreateElement("span");
			quote.ClassList.Add("quote-inline");

			if (hasContent)
			{
				quote.AppendChild(document.CreateElement("br"));
				quote.AppendChild(document.CreateElement("br"));
			}

			var re = document.CreateElement("span");
			re.TextContent = "RE: ";
			quote.AppendChild(re);
			quote.AppendChild(a);
			element.AppendChild(quote);
		}
		else if (quoteInaccessible)
		{
			var wrapper = document.CreateElement("span");
			var re      = document.CreateElement("span");
			re.TextContent = "RE: \ud83d\udd12"; // lock emoji

			if (hasContent)
			{
				wrapper.AppendChild(document.CreateElement("br"));
				wrapper.AppendChild(document.CreateElement("br"));
			}

			wrapper.AppendChild(re);
			element.AppendChild(wrapper);
		}

		await using var sw = new StringWriter();
		await element.ToHtmlAsync(sw);
		return sw.ToString();
	}

	public async Task<string> ToHtmlAsync(
		string mfm, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "p",
		List<Emoji>? emoji = null
	)
	{
		var nodes = MfmParser.Parse(mfm);
		return await ToHtmlAsync(nodes, mentions, host, quoteUri, quoteInaccessible,
		                         replyInaccessible, rootElement, emoji);
	}

	private INode FromMfmNode(
		IDocument document, IMfmNode node, List<Note.MentionedUser> mentions, string? host, List<Emoji>? emoji = null
	)
	{
		switch (node)
		{
			case MfmBoldNode:
			{
				var el = CreateInlineFormattingElement(document, "b");
				AddHtmlMarkup(document, el, "**");
				AppendChildren(el, document, node, mentions, host);
				AddHtmlMarkup(document, el, "**");
				return el;
			}
			case MfmSmallNode:
			{
				var el = document.CreateElement("small");
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			case MfmStrikeNode:
			{
				var el = CreateInlineFormattingElement(document, "del");
				AddHtmlMarkup(document, el, "~~");
				AppendChildren(el, document, node, mentions, host);
				AddHtmlMarkup(document, el, "~~");
				return el;
			}
			case MfmItalicNode:
			case MfmFnNode:
			{
				var el = CreateInlineFormattingElement(document, "i");
				AddHtmlMarkup(document, el, "*");
				AppendChildren(el, document, node, mentions, host);
				AddHtmlMarkup(document, el, "*");
				return el;
			}
			case MfmCodeBlockNode codeBlockNode:
			{
				var el    = CreateInlineFormattingElement(document, "pre");
				var inner = CreateInlineFormattingElement(document, "code");
				inner.TextContent = codeBlockNode.Code;
				el.AppendNodes(inner);
				return el;
			}
			case MfmCenterNode:
			{
				var el = document.CreateElement("div");
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			case MfmEmojiCodeNode emojiCodeNode:
			{
				var punyHost = host?.ToPunycodeLower();
				if (emoji?.FirstOrDefault(p => p.Name == emojiCodeNode.Name && p.Host == punyHost) is { } hit)
				{
					var el    = document.CreateElement("span");
					var inner = document.CreateElement("img");
					inner.SetAttribute("src", hit.PublicUrl);
					inner.SetAttribute("alt", hit.Name);
					el.AppendChild(inner);
					el.ClassList.Add("emoji");
					return el;
				}

				return document.CreateTextNode($"\u200B:{emojiCodeNode.Name}:\u200B");
			}
			case MfmHashtagNode hashtagNode:
			{
				var el = document.CreateElement("a");
				el.SetAttribute("href", $"https://{config.Value.WebDomain}/tags/{hashtagNode.Hashtag}");
				el.TextContent = $"#{hashtagNode.Hashtag}";
				el.SetAttribute("rel", "tag");
				return el;
			}
			case MfmInlineCodeNode inlineCodeNode:
			{
				var el = CreateInlineFormattingElement(document, "code");
				el.TextContent = inlineCodeNode.Code;
				return el;
			}
			case MfmInlineMathNode inlineMathNode:
			{
				var el = CreateInlineFormattingElement(document, "code");
				el.TextContent = inlineMathNode.Formula;
				return el;
			}
			case MfmMathBlockNode mathBlockNode:
			{
				var el = CreateInlineFormattingElement(document, "code");
				el.TextContent = mathBlockNode.Formula;
				return el;
			}
			case MfmLinkNode linkNode:
			{
				var el = document.CreateElement("a");
				el.SetAttribute("href", linkNode.Url);
				el.TextContent = linkNode.Text;
				return el;
			}
			case MfmMentionNode mentionNode:
			{
				var el = document.CreateElement("span");

				// Fall back to object host, as localpart-only mentions are relative to the instance the note originated from
				var finalHost = mentionNode.Host ?? host ?? config.Value.AccountDomain;

				if (finalHost == config.Value.WebDomain)
					finalHost = config.Value.AccountDomain;

				Func<Note.MentionedUser, bool> predicate = finalHost == config.Value.AccountDomain
					? p => p.Username.EqualsIgnoreCase(mentionNode.User)
					       && (p.Host.EqualsIgnoreCase(finalHost) || p.Host == null)
					: p => p.Username.EqualsIgnoreCase(mentionNode.User) && p.Host.EqualsIgnoreCase(finalHost);

				if (mentions.FirstOrDefault(predicate) is not { } mention)
				{
					el.TextContent = $"@{mentionNode.Acct}";
				}
				else
				{
					el.ClassList.Add("h-card");
					el.SetAttribute("translate", "no");
					var a = document.CreateElement("a");
					a.ClassList.Add("u-url", "mention");
					a.SetAttribute("href", mention.Url ?? mention.Uri);
					var span = document.CreateElement("span");
					span.TextContent = $"@{mention.Username}";
					a.AppendChild(span);
					el.AppendChild(a);
				}

				return el;
			}
			case MfmQuoteNode:
			{
				var el = CreateInlineFormattingElement(document, "blockquote");
				AddHtmlMarkup(document, el, "> ");
				AppendChildren(el, document, node, mentions, host);
				el.AppendChild(document.CreateElement("br"));
				return el;
			}
			case MfmTextNode textNode:
			{
				var el = document.CreateElement("span");
				var nodes = textNode.Text.Split("\r\n")
				                    .SelectMany(p => p.Split('\r'))
				                    .SelectMany(p => p.Split('\n'))
				                    .Select(document.CreateTextNode);

				foreach (var htmlNode in nodes)
				{
					el.AppendNodes(htmlNode);
					el.AppendNodes(document.CreateElement("br"));
				}

				if (el.LastChild != null)
					el.RemoveChild(el.LastChild);
				return el;
			}
			case MfmUrlNode urlNode:
			{
				var el = document.CreateElement("a");
				el.SetAttribute("href", urlNode.Url);
				var prefix = urlNode.Url.StartsWith("https://") ? "https://" : "http://";
				var length = prefix.Length;
				el.TextContent = urlNode.Url[length..];
				return el;
			}
			case MfmPlainNode:
			{
				var el = document.CreateElement("span");
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			default:
			{
				throw new NotImplementedException("Unsupported MfmNode type");
			}
		}
	}

	private void AppendChildren(
		INode element, IDocument document, IMfmNode parent,
		List<Note.MentionedUser> mentions, string? host, List<Emoji>? emoji = null
	)
	{
		foreach (var node in parent.Children) element.AppendNodes(FromMfmNode(document, node, mentions, host, emoji));
	}

	private IElement CreateInlineFormattingElement(IDocument document, string name)
	{
		return document.CreateElement(SupportsHtmlFormatting.Value ? name : "span");
	}

	private void AddHtmlMarkup(IDocument document, IElement node, string chars)
	{
		if (SupportsHtmlFormatting.Value) return;
		var el = document.CreateElement("span");
		el.AppendChild(document.CreateTextNode(chars));
		node.AppendChild(el);
	}
}
