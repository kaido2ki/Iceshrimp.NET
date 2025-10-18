using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Iceshrimp.MfmSharp;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.MfmSharp.Helpers;
using Microsoft.Extensions.Options;
using MfmHtmlParser = Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing.HtmlParser;
using HtmlParser = AngleSharp.Html.Parser.HtmlParser;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;

public readonly record struct MfmInlineMedia(MfmInlineMedia.MediaType Type, string Src, string? Alt)
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

/// <summary>Resulting data after HTML to MFM conversion</summary>
public readonly record struct HtmlMfmData(string Mfm, List<MfmInlineMedia> InlineMedia);

/// <summary>Resulting data after MFM to HTML conversion</summary>
public readonly record struct MfmHtmlData(string Html, List<MfmInlineMedia> InlineMedia);

public class MfmConverter(
	IOptions<Config.InstanceSection> config,
	MediaProxyService mediaProxy,
	FlagService flags
) : ISingletonService
{
	private static readonly HtmlParser Parser = new();

	private static readonly Lazy<IHtmlDocument> OwnerDocument =
		new(() => Parser.ParseDocument(ReadOnlyMemory<char>.Empty));

	private static IElement CreateElement(string name)  => OwnerDocument.Value.CreateElement(name);
	private static IText    CreateTextNode(string data) => OwnerDocument.Value.CreateTextNode(data);

	public static HtmlMfmData FromHtml(
		string? html, List<Note.MentionedUser>? mentions = null, List<string>? hashtags = null
	)
	{
		var media = new List<MfmInlineMedia>();
		if (html == null) return new HtmlMfmData("", media);

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>(?:\r?\n)?", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		// Ensure compatibility with AP servers that send non-breaking space characters instead of regular spaces
		html = html.Replace("\u00A0", " ");

		// Ensure compatibility with AP servers that send CRLF or CR instead of LF-style newlines
		html = html.ReplaceLineEndings("\n");

		var dom = Parser.ParseDocument(html);
		if (dom.Body == null) return new HtmlMfmData("", media);

		var sb     = new StringBuilder();
		var parser = new MfmHtmlParser(mentions ?? [], hashtags ?? [], media);
		dom.Body.ChildNodes.Select(parser.ParseNode).ToList().ForEach(s => sb.Append(s));
		return new HtmlMfmData(sb.ToString().Trim(), media);
	}

	public static List<string> ExtractMentionsFromHtml(string? html)
	{
		if (html == null) return [];

		// Ensure compatibility with AP servers that send both <br> as well as newlines
		var regex = new Regex(@"<br\s?\/?>\r?\n", RegexOptions.IgnoreCase);
		html = regex.Replace(html, "\n");

		var dom = Parser.ParseDocument(html);
		if (dom.Body == null) return [];

		var parser = new HtmlMentionsExtractor();
		foreach (var node in dom.Body.ChildNodes)
			parser.ParseChildren(node);

		return parser.Mentions;
	}

	public MfmHtmlData ToHtml(
		IMfmNode[] nodes, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "p",
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		var element    = CreateElement(rootElement);
		var hasContent = nodes.Length > 0;

		if (replyInaccessible)
		{
			var wrapper = CreateElement("span");
			var re      = CreateElement("span");
			re.TextContent = "RE: \ud83d\udd12"; // lock emoji
			wrapper.AppendChild(re);

			if (hasContent)
			{
				wrapper.AppendChild(CreateElement("br"));
				wrapper.AppendChild(CreateElement("br"));
			}

			element.AppendChild(wrapper);
		}

		var usedMedia = new List<MfmInlineMedia>();
		foreach (var node in nodes)
			element.AppendNodes(FromMfmNode(node, mentions, host, usedMedia, emoji, media));

		if (quoteUri != null)
		{
			var a = CreateElement("a");
			a.SetAttribute("href", quoteUri);
			a.TextContent = quoteUri.StartsWith("https://") ? quoteUri[8..] : quoteUri[7..];
			var quote = CreateElement("span");
			quote.ClassList.Add("quote-inline");

			if (hasContent)
			{
				quote.AppendChild(CreateElement("br"));
				quote.AppendChild(CreateElement("br"));
			}

			var re = CreateElement("span");
			re.TextContent = "RE: ";
			quote.AppendChild(re);
			quote.AppendChild(a);
			element.AppendChild(quote);
		}
		else if (quoteInaccessible)
		{
			var wrapper = CreateElement("span");
			var re      = CreateElement("span");
			re.TextContent = "RE: \ud83d\udd12"; // lock emoji

			if (hasContent)
			{
				wrapper.AppendChild(CreateElement("br"));
				wrapper.AppendChild(CreateElement("br"));
			}

			wrapper.AppendChild(re);
			element.AppendChild(wrapper);
		}

		return new MfmHtmlData(element.ToHtml(), usedMedia);
	}

	public MfmHtmlData ToHtml(
		string mfm, List<Note.MentionedUser> mentions, string? host, string? quoteUri = null,
		bool quoteInaccessible = false, bool replyInaccessible = false, string rootElement = "div",
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		var nodes = MfmParser.Parse(mfm);
		return ToHtml(nodes, mentions, host, quoteUri, quoteInaccessible, replyInaccessible, rootElement, emoji, media);
	}

	public string ProfileFieldToHtml(MfmUrlNode node)
	{
		var parsed = FromMfmNode(node, [], null, []);
		if (parsed is not IHtmlAnchorElement el)
			return parsed.ToHtml();

		el.SetAttribute("rel", "me nofollow noopener");
		el.SetAttribute("target", "_blank");

		return el.ToHtml();
	}

	private INode FromMfmNode(
		IMfmNode node, List<Note.MentionedUser> mentions, string? host, List<MfmInlineMedia> usedMedia,
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		switch (node)
		{
			case MfmFnNode { Name: "media" } fn when media is { Count: > 0 }:
			{
				var urlNode = fn.Children.FirstOrDefault();
				if (urlNode is MfmUrlNode url)
				{
					MfmInlineMedia? maybeCurrent = media.FirstOrDefault(m => m.Src == url.Url);
					if (maybeCurrent is { } current)
					{
						usedMedia.Add(current);

						if (!flags.SupportsInlineMedia.Value || current.Type == MfmInlineMedia.MediaType.Other)
						{
							var el = CreateElement("a");
							el.SetAttribute("href", current.Src);

							if (current.Type == MfmInlineMedia.MediaType.Other)
								el.SetAttribute("download", "true");

							var icon = current.Type switch
							{
								MfmInlineMedia.MediaType.Image => "\ud83d\uddbc\ufe0f", // framed picture emoji
								MfmInlineMedia.MediaType.Video => "\ud83c\udfac",       // clapperboard emoji
								MfmInlineMedia.MediaType.Audio => "\ud83c\udfb5",       // music note emoji
								_                              => "\ud83d\udcbe",       // floppy disk emoji
							};

							el.TextContent = $"[{icon} {current.Alt ?? current.Src}]";
							return el;
						}
						else
						{
							var nodeName = current.Type switch
							{
								MfmInlineMedia.MediaType.Image => "img",
								MfmInlineMedia.MediaType.Video => "video",
								MfmInlineMedia.MediaType.Audio => "audio",
								_                              => throw new ArgumentOutOfRangeException()
							};

							var el = CreateElement(nodeName);
							el.SetAttribute("src", current.Src);
							el.SetAttribute("alt", current.Alt);
							return el;
						}
					}
				}

				{
					var el = CreateInlineFormattingElement("i");
					AddHtmlMarkup(el, "*");
					AppendChildren(el, node, mentions, host, usedMedia);
					AddHtmlMarkup(el, "*");
					return el;
				}
			}
			case MfmFnNode { Name: "unixtime" } fn:
			{
				var el = CreateInlineFormattingElement("i");

				if (fn.Children.Length != 1 || fn.Children.FirstOrDefault() is not MfmTextNode textNode)
					return Fallback();

				double timestamp;
				if (!double.TryParse(textNode.Text, out timestamp)) return Fallback();

				var date = DateTime.UnixEpoch.AddSeconds(timestamp);
				el.TextContent = date.ToString("HH:mm, d MMM yyyy") + " UTC";

				return el;

				IElement Fallback()
				{
					AddHtmlMarkup(el, "*");
					AppendChildren(el, node, mentions, host, usedMedia);
					AddHtmlMarkup(el, "*");
					return el;
				}
			}
            case MfmFnNode { Name: "ruby" } fn:
            {
                var el = CreateInlineFormattingElement("ruby");

                if (fn.Children.FirstOrDefault() is not {} first)
                    return Fallback();

                // TODO: this wont work for bold and italic and whatnot, misskey supports those for the first part at least
                if (fn.Children.Length == 1 && first is MfmTextNode firstText)
                {
                    var parts = firstText.Text.Split(' ', count: 3);
                    if (parts.Length == 0) return Fallback();

                    el.AppendChild(CreateTextNode(parts[0]));
                    var secondPart = parts.ElementAtOrDefault(1);
                    if (!string.IsNullOrWhiteSpace(secondPart))
                    {
                        var rt = CreateInlineFormattingElement("rt");
                        rt.AppendChild(CreateTextNode(secondPart));
                        el.AppendChild(rt);
                    }
                }
                else
                {
                    Fallback();
                }

                return el;

                IElement Fallback()
                {
                    AddHtmlMarkup(el, "*");
                    AppendChildren(el, node, mentions, host, usedMedia);
                    AddHtmlMarkup(el, "*");
                    return el;
                }
            }
            case MfmFnNode fn:
            {
                var el = CreateInlineFormattingElement("span");
                el.ClassList.Add($"mfm-{fn.Name}");

                if (fn.Args != null)
                {
                    foreach (var (key, value) in fn.Args)
                    {
                        // this should be safe given argument keys only support letters and digits
                        // https://iceshrimp.dev/iceshrimp/Iceshrimp.MfmSharp/src/branch/dev/Iceshrimp.MfmSharp/MfmParser.cs#L572
                        el.SetAttribute($"data-mfm-{key}", value);
                    }
                }
                
                AppendChildren(el, node, mentions, host, usedMedia);
                return el;
            }
			case MfmBoldNode:
			{
				var el = CreateInlineFormattingElement("b");
				AddHtmlMarkup(el, "**");
				AppendChildren(el, node, mentions, host, usedMedia);
				AddHtmlMarkup(el, "**");
				return el;
			}
			case MfmSmallNode:
			{
				var el = CreateElement("small");
				AppendChildren(el, node, mentions, host, usedMedia);
				return el;
			}
			case MfmStrikeNode:
			{
				var el = CreateInlineFormattingElement("del");
				AddHtmlMarkup(el, "~~");
				AppendChildren(el, node, mentions, host, usedMedia);
				AddHtmlMarkup(el, "~~");
				return el;
			}
			case MfmItalicNode:
			{
				var el = CreateInlineFormattingElement("i");
				AddHtmlMarkup(el, "*");
				AppendChildren(el, node, mentions, host, usedMedia);
				AddHtmlMarkup(el, "*");
				return el;
			}
			case MfmCodeBlockNode codeBlockNode:
			{
				var el = CreateInlineFormattingElement("pre");

				if (flags.SupportsHtmlFormatting.Value)
				{
					var inner = CreateInlineFormattingElement("code");
					inner.TextContent = codeBlockNode.Code;
					el.AppendNodes(inner);
				}
				else
				{
					var split = codeBlockNode.Code.Split('\n');
					for (var index = 0; index < split.Length; index++)
					{
						var line = split[index];
						el.AppendNodes(CreateTextNode(line));
						if (index < split.Length - 1)
							el.AppendNodes(CreateElement("br"));
					}
				}

				return el;
			}
			case MfmCenterNode:
			{
				var el = CreateInlineFormattingElement("center");
				AppendChildren(el, node, mentions, host, usedMedia);
				return el;
			}
			case MfmEmojiCodeNode emojiCodeNode:
			{
				var punyHost = host?.ToPunycodeLower();
				if (emoji?.FirstOrDefault(p => p.Name == emojiCodeNode.Name && p.Host == punyHost) is { } hit)
				{
					var el    = CreateElement("span");
					var inner = CreateElement("img");
					inner.SetAttribute("src", mediaProxy.GetProxyUrl(hit));
					inner.SetAttribute("alt", hit.Name);
					el.AppendChild(inner);
					el.ClassList.Add("emoji");
					return el;
				}

				return CreateTextNode($"\u200B:{emojiCodeNode.Name}:\u200B");
			}
			case MfmHashtagNode hashtagNode:
			{
				var el = CreateElement("a");
				el.SetAttribute("href", $"https://{config.Value.WebDomain}/tags/{hashtagNode.Hashtag}");
				el.TextContent = $"#{hashtagNode.Hashtag}";
				el.SetAttribute("rel", "tag");
				el.ClassList.Add("hashtag");
				return el;
			}
			case MfmInlineCodeNode inlineCodeNode:
			{
				var el = CreateInlineFormattingElement("code");
				el.TextContent = inlineCodeNode.Code;
				return el;
			}
			case MfmInlineMathNode inlineMathNode:
			{
				var el = CreateInlineFormattingElement("code");
				el.TextContent = inlineMathNode.Formula;
				return el;
			}
			case MfmMathBlockNode mathBlockNode:
			{
				var el = CreateInlineFormattingElement("code");
				el.TextContent = mathBlockNode.Formula;
				return el;
			}
			case MfmLinkNode linkNode:
			{
				var el = CreateElement("a");
				el.SetAttribute("href", linkNode.Url);
				el.TextContent = linkNode.Text;
				return el;
			}
			case MfmMentionNode mentionNode:
			{
				var el = CreateElement("span");

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
					var a = CreateElement("a");
					a.ClassList.Add("u-url", "mention");
					a.SetAttribute("href", mention.Url ?? mention.Uri);
					var span = CreateElement("span");
					span.TextContent = $"@{mention.Username}";
					a.AppendChild(span);
					el.AppendChild(a);
				}

				return el;
			}
			case MfmQuoteNode:
			{
				var el = CreateInlineFormattingElement("blockquote");
				AddHtmlMarkup(el, "> ");
				AppendChildren(el, node, mentions, host, usedMedia);
				AddHtmlMarkupTag(el, "br");
				AddHtmlMarkupTag(el, "br");
				return el;
			}
			case MfmTextNode textNode:
			{
				var el = CreateElement("span");
				var nodes = textNode.Text.Split("\r\n")
				                    .SelectMany(p => p.Split('\r'))
				                    .SelectMany(p => p.Split('\n'))
				                    .Select(CreateTextNode);

				foreach (var htmlNode in nodes)
				{
					el.AppendNodes(htmlNode);
					el.AppendNodes(CreateElement("br"));
				}

				if (el.LastChild != null)
					el.RemoveChild(el.LastChild);
				return el;
			}
			case MfmUrlNode urlNode:
			{
				if (
					!Uri.TryCreate(urlNode.Url, UriKind.Absolute, out var uri)
					|| uri is not { Scheme: "http" or "https" }
				)
				{
					var fallbackEl = CreateElement("span");
					fallbackEl.TextContent = urlNode.Url;
					return fallbackEl;
				}

				var el = CreateElement("a");
				el.SetAttribute("href", urlNode.Url);
				el.TextContent = uri.ToMfmDisplayString();
				return el;
			}
			case MfmPlainNode:
			{
				var el = CreateElement("span");
				AppendChildren(el, node, mentions, host, usedMedia);
				return el;
			}
			default:
			{
				throw new NotImplementedException("Unsupported MfmNode type");
			}
		}
	}

	private void AppendChildren(
		INode element, IMfmNode parent,
		List<Note.MentionedUser> mentions, string? host, List<MfmInlineMedia> usedMedia,
		List<Emoji>? emoji = null, List<MfmInlineMedia>? media = null
	)
	{
		foreach (var node in parent.Children)
			element.AppendNodes(FromMfmNode(node, mentions, host, usedMedia, emoji, media));
	}

	private IElement CreateInlineFormattingElement(string name)
	{
		return CreateElement(flags.SupportsHtmlFormatting.Value ? name : "span");
	}

	private void AddHtmlMarkup(IElement node, string chars)
	{
		if (flags.SupportsHtmlFormatting.Value) return;
		var el = CreateElement("span");
		el.AppendChild(CreateTextNode(chars));
		node.AppendChild(el);
	}
	
	private void AddHtmlMarkupTag(IElement node, string tag)
	{
		if (flags.SupportsHtmlFormatting.Value) return;
		var el = CreateElement(tag);
		node.AppendChild(el);
	}
}
