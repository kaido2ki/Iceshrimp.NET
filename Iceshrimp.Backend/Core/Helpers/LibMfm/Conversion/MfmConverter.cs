using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Microsoft.Extensions.Options;
using Microsoft.FSharp.Collections;
using static Iceshrimp.Parsing.MfmNodeTypes;
using MfmHtmlParser = Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing.HtmlParser;
using HtmlParser = AngleSharp.Html.Parser.HtmlParser;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;

public record MfmInlineMedia(MfmInlineMedia.MediaType Type, string Src, string? Alt)
{
	public enum MediaType
	{
		Other,
		Image,
		Video,
		Audio
	}
	
	public static MediaType GetType(string mime)
	{
		if (mime.StartsWith("image/")) return MediaType.Image;
		if (mime.StartsWith("video/")) return MediaType.Video;
		if (mime.StartsWith("audio/")) return MediaType.Audio;

		return MediaType.Other;
	}
}

public class MfmConverter(
	IOptions<Config.InstanceSection> config
) : ISingletonService
{
	public AsyncLocal<bool> SupportsHtmlFormatting { get; } = new();

	public static async Task<(string Mfm, List<MfmInlineMedia> InlineMedia)> FromHtmlAsync(string? html, List<Note.MentionedUser>? mentions = null)
	{
		var media = new List<MfmInlineMedia>();
		if (html == null) return ("", media);

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		// Ensure compatibility with AP servers that send non-breaking space characters instead of regular spaces
		html = html.Replace("\u00A0", " ");

		var dom = await new HtmlParser().ParseDocumentAsync(html);
		if (dom.Body == null) return ("", media);

		var sb     = new StringBuilder();
		var parser = new MfmHtmlParser(mentions ?? [], media);
		dom.Body.ChildNodes.Select(parser.ParseNode).ToList().ForEach(s => sb.Append(s));
		return (sb.ToString().Trim(), media);
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

	public async Task<(string Html, List<MfmInlineMedia> InlineMedia)> ToHtmlAsync(
		IEnumerable<MfmNode> nodes, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "p",
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
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

		var usedMedia = new List<MfmInlineMedia>();
		foreach (var node in nodeList) element.AppendNodes(FromMfmNode(document, node, mentions, host, ref usedMedia, emoji, media));

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
		return (sw.ToString(), usedMedia);
	}

	public async Task<(string Html, List<MfmInlineMedia> InlineMedia)> ToHtmlAsync(
		string mfm, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "p",
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		var nodes = MfmParser.Parse(mfm);
		return await ToHtmlAsync(nodes, mentions, host, quoteUri, quoteInaccessible,
		                         replyInaccessible, rootElement, emoji, media);
	}

	private INode FromMfmNode(
		IDocument document, MfmNode node, List<Note.MentionedUser> mentions, string? host, ref List<MfmInlineMedia> usedMedia, 
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		switch (node)
		{
			case MfmFnNode { Name: "media" } fn when media != null:
			{
				var urlNode = fn.Children.HeadOrDefault;
				if (urlNode is MfmUrlNode url)
				{
					var current = media.FirstOrDefault(m => m.Src == url.Url);
					if (current != null)
					{
						var nodeName = current.Type switch
						{
							MfmInlineMedia.MediaType.Image   => "img",
							MfmInlineMedia.MediaType.Video   => "video",
							MfmInlineMedia.MediaType.Audio   => "audio",
							_                                => "a",
						};
						var el = document.CreateElement(nodeName);
						if (current.Type == MfmInlineMedia.MediaType.Other)
						{
							el.SetAttribute("href", current.Src);
							el.SetAttribute("download", "true");
							el.TextContent = $"\ud83d\udcbe {current.Alt ?? current.Src}"; // floppy disk emoji
						}
						else
						{
							el.SetAttribute("src", current.Src);
							el.SetAttribute("alt", current.Alt);
						}
						
						usedMedia.Add(current);
						return el;
					}
				}

				var fallbackEl = CreateInlineFormattingElement(document, "i");
				AddHtmlMarkup(node, "*");
				AppendChildren(fallbackEl, document, node, mentions, host, ref usedMedia);
				return fallbackEl;
			}
			case MfmBoldNode:
			{
				var el = CreateInlineFormattingElement(document, "b");
				AddHtmlMarkup(node, "**");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
				return el;
			}
			case MfmSmallNode:
			{
				var el = document.CreateElement("small");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
				return el;
			}
			case MfmStrikeNode:
			{
				var el = CreateInlineFormattingElement(document, "del");
				AddHtmlMarkup(node, "~~");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
				return el;
			}
			case MfmItalicNode:
			case MfmFnNode:
			{
				var el = CreateInlineFormattingElement(document, "i");
				AddHtmlMarkup(node, "*");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
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
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
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
			case MfmMathInlineNode mathInlineNode:
			{
				var el = CreateInlineFormattingElement(document, "code");
				el.TextContent = mathInlineNode.Formula;
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
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
				return el;
			}
			case MfmMentionNode mentionNode:
			{
				var el = document.CreateElement("span");

				// Fall back to object host, as localpart-only mentions are relative to the instance the note originated from
				var finalHost = mentionNode.Host?.Value ?? host ?? config.Value.AccountDomain;

				if (finalHost == config.Value.WebDomain)
					finalHost = config.Value.AccountDomain;

				Func<Note.MentionedUser, bool> predicate = finalHost == config.Value.AccountDomain
					? p => p.Username.EqualsIgnoreCase(mentionNode.Username) &&
					       (p.Host.EqualsIgnoreCase(finalHost) || p.Host == null)
					: p => p.Username.EqualsIgnoreCase(mentionNode.Username) &&
					       p.Host.EqualsIgnoreCase(finalHost);

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
				AddHtmlMarkupStartOnly(node, "> ");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
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
			case MfmSearchNode searchNode:
			{
				//TODO: get search engine from config
				var el = document.CreateElement("a");
				el.SetAttribute("href", $"https://duckduckgo.com?q={HttpUtility.UrlEncode(searchNode.Query)}");
				el.TextContent = searchNode.Content;
				return el;
			}
			case MfmPlainNode:
			{
				var el = document.CreateElement("span");
				AppendChildren(el, document, node, mentions, host, ref usedMedia);
				return el;
			}
			default:
			{
				throw new NotImplementedException("Unsupported MfmNode type");
			}
		}
	}

	private void AppendChildren(
		INode element, IDocument document, MfmNode parent,
		List<Note.MentionedUser> mentions, string? host, ref List<MfmInlineMedia> usedMedia, List<Emoji>? emoji = null,
		List<MfmInlineMedia>? media = null
	)
	{
		foreach (var node in parent.Children) element.AppendNodes(FromMfmNode(document, node, mentions, host, ref usedMedia, emoji, media));
	}

	private IElement CreateInlineFormattingElement(IDocument document, string name)
	{
		return document.CreateElement(SupportsHtmlFormatting.Value ? name : "span");
	}

	private void AddHtmlMarkup(MfmNode node, string chars)
	{
		if (SupportsHtmlFormatting.Value) return;
		var markupNode = new MfmTextNode(chars);
		node.Children = ListModule.OfSeq(node.Children.Prepend(markupNode).Append(markupNode));
	}

	private void AddHtmlMarkupStartOnly(MfmNode node, string chars)
	{
		if (SupportsHtmlFormatting.Value) return;
		var markupNode = new MfmTextNode(chars);
		node.Children = ListModule.OfSeq(node.Children.Prepend(markupNode));
	}
}