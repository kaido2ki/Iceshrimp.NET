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
using Iceshrimp.Backend.Core.Helpers.LibMfm.Types;
using Microsoft.Extensions.Options;
using MfmHtmlParser = Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing.HtmlParser;
using HtmlParser = AngleSharp.Html.Parser.HtmlParser;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;

public class MfmConverter(IOptions<Config.InstanceSection> config)
{
	public async Task<string?> FromHtmlAsync(string? html, List<Note.MentionedUser>? mentions = null)
	{
		if (html == null) return null;

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		var dom = await new HtmlParser().ParseDocumentAsync(html);
		if (dom.Body == null) return "";

		var sb     = new StringBuilder();
		var parser = new MfmHtmlParser(mentions ?? []);
		dom.Body.ChildNodes.Select(parser.ParseNode).ToList().ForEach(s => sb.Append(s));
		return sb.ToString().Trim();
	}

	public async Task<string> ToHtmlAsync(IEnumerable<MfmNode> nodes, List<Note.MentionedUser> mentions, string? host)
	{
		var context  = BrowsingContext.New();
		var document = await context.OpenNewAsync();
		var element  = document.CreateElement("p");

		foreach (var node in nodes) element.AppendNodes(FromMfmNode(document, node, mentions, host));

		await using var sw = new StringWriter();
		await element.ToHtmlAsync(sw);
		return sw.ToString();
	}

	public async Task<string> ToHtmlAsync(string mfm, List<Note.MentionedUser> mentions, string? host)
	{
		var nodes = MfmParser.Parse(mfm);
		return await ToHtmlAsync(nodes, mentions, host);
	}

	private INode FromMfmNode(IDocument document, MfmNode node, List<Note.MentionedUser> mentions, string? host)
	{
		switch (node)
		{
			case MfmBoldNode:
			{
				var el = document.CreateElement("b");
				AppendChildren(el, document, node, mentions, host);
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
				var el = document.CreateElement("del");
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			case MfmItalicNode:
			case MfmFnNode:
			{
				var el = document.CreateElement("i");
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			case MfmCodeBlockNode codeBlockNode:
			{
				var el    = document.CreateElement("pre");
				var inner = document.CreateElement("code");
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
				return document.CreateTextNode($"\u200B:{emojiCodeNode.Name}:\u200B");
			}
			case MfmUnicodeEmojiNode unicodeEmojiNode:
			{
				return document.CreateTextNode(unicodeEmojiNode.Emoji);
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
				var el = document.CreateElement("code");
				el.TextContent = inlineCodeNode.Code;
				return el;
			}
			case MfmMathInlineNode mathInlineNode:
			{
				var el = document.CreateElement("code");
				el.TextContent = mathInlineNode.Formula;
				return el;
			}
			case MfmMathBlockNode mathBlockNode:
			{
				var el = document.CreateElement("code");
				el.TextContent = mathBlockNode.Formula;
				return el;
			}
			case MfmLinkNode linkNode:
			{
				var el = document.CreateElement("a");
				el.SetAttribute("href", linkNode.Url);
				AppendChildren(el, document, node, mentions, host);
				return el;
			}
			case MfmMentionNode mentionNode:
			{
				var el = document.CreateElement("span");

				// Fall back to object host, as localpart-only mentions are relative to the instance the note originated from
				mentionNode.Host ??= host ?? config.Value.AccountDomain;

				if (mentionNode.Host == config.Value.WebDomain)
					mentionNode.Host = config.Value.AccountDomain;

				var mention = mentions.FirstOrDefault(p => p.Username.EqualsIgnoreCase(mentionNode.Username) &&
				                                           p.Host.EqualsIgnoreCase(mentionNode.Host));
				if (mention == null)
				{
					el.TextContent = mentionNode.Acct;
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
				var el = document.CreateElement("blockquote");
				AppendChildren(el, document, node, mentions, host);
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
		INode element, IDocument document, MfmNode parent,
		List<Note.MentionedUser> mentions, string? host
	)
	{
		foreach (var node in parent.Children) element.AppendNodes(FromMfmNode(document, node, mentions, host));
	}
}