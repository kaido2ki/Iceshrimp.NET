using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;

internal class HtmlParser(IEnumerable<Note.MentionedUser> mentions, ICollection<MfmInlineMedia> media)
{
	internal string? ParseNode(INode node)
	{
		if (node.NodeType is NodeType.Text)
			return node.TextContent;
		if (node.NodeType is NodeType.Comment or NodeType.Document)
			return null;

		switch (node.NodeName)
		{
			case "BR":
			{
				return "\n";
			}
			case "A":
			{
				if (node is not HtmlElement el) return node.TextContent;

				var href = el.GetAttribute("href");
				if (href == null) return $"<plain>{el.TextContent}</plain>";

				if (el.ClassList.Contains("u-url") && el.ClassList.Contains("mention"))
				{
					var mention = mentions.FirstOrDefault(p => p.Uri == href || p.Url == href);
					return mention != null
						? $"@{mention.Username}@{mention.Host}"
						: $"<plain>{el.TextContent}</plain>";
				}

				if (el.TextContent == href && (href.StartsWith("http://") || href.StartsWith("https://")))
					return href;

				return $"[{el.TextContent}]({href})";
			}
			case "H1":
			{
				return $"【{ParseChildren(node)}】\n";
			}
			case "B":
			case "STRONG":
			{
				return $"<b>{ParseChildren(node)}</b>";
			}
			case "SMALL":
			{
				return $"<small>{ParseChildren(node)}</small>";
			}
			case "S":
			case "DEL":
			{
				return $"<s>{ParseChildren(node)}</s>";
			}
			case "I":
			case "EM":
			{
				return $"<i>{ParseChildren(node)}</i>";
			}
			case "PRE":
			{
				return node.ChildNodes is [{ NodeName: "CODE" }]
					? $"\n```\n{string.Join(null, node.ChildNodes[0].TextContent)}\n```\n"
					: ParseChildren(node);
			}
			case "CODE":
			{
				return $"`{ParseChildren(node)}`";
			}
			case "BLOCKQUOTE":
			{
				return node.TextContent.Length > 0
					? $"\n> {string.Join("\n> ", node.TextContent.Split("\n"))}"
					: null;
			}

			case "VIDEO":
			case "AUDIO":
			case "IMG":
			{
				if (node is not HtmlElement el) return node.TextContent;

				var src = el.GetAttribute("src");
				if (src == null || !Uri.TryCreate(src, UriKind.Absolute, out var uri) && uri is { Scheme: "http" or "https" })
					return node.TextContent;

				var alt = el.GetAttribute("alt") ?? el.GetAttribute("title");

				var type = node.NodeName switch
				{
					"VIDEO" => MfmInlineMedia.MediaType.Video,
					"AUDIO" => MfmInlineMedia.MediaType.Audio,
					"IMG"   => MfmInlineMedia.MediaType.Image,
					_       => MfmInlineMedia.MediaType.Other,
				};

				media.Add(new MfmInlineMedia(type, src, alt));

				return $"$[media {src}]";
			}

			case "P":
			case "H2":
			case "H3":
			case "H4":
			case "H5":
			case "H6":
			{
				return $"\n\n{ParseChildren(node)}";
			}

			case "DIV":
			case "HEADER":
			case "FOOTER":
			case "ARTICLE":
			case "LI":
			case "DT":
			case "DD":
			{
				return $"\n{ParseChildren(node)}";
			}

			default:
			{
				return ParseChildren(node);
			}
		}
	}

	private string ParseChildren(INode node)
	{
		return string.Join(null, node.ChildNodes.Select(ParseNode));
	}
}