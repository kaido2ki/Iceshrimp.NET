using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Text;
using Iceshrimp.MfmSharp;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public static partial class MfmRenderer
{
	public static async Task<MarkupString> RenderStringAsync(
		string text, List<EmojiResponse> emoji, string accountDomain, bool simple = false
	)
	{
		var res         = MfmParser.Parse(text, simple);
		var context     = BrowsingContext.New();
		var document    = await context.OpenNewAsync();
		var renderedMfm = RenderMultipleNodes(res, document, emoji, accountDomain, simple);
		var html        = renderedMfm.ToHtml();
		return new MarkupString(html);
	}

	private static INode RenderMultipleNodes(
		IEnumerable<IMfmNode> nodes, IDocument document, List<EmojiResponse> emoji, string accountDomain, bool simple
	)
	{
		var el = document.CreateElement("span");
		el.SetAttribute("mfm", "mfm");
		el.ClassName = "mfm";
		foreach (var node in nodes)
		{
			try
			{
				el.AppendNodes(RenderNode(node, document, emoji, accountDomain, simple));
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
		IMfmNode node, IDocument document, List<EmojiResponse> emoji, string accountDomain, bool simple
	)
	{
		// Hard wrap makes this impossible to read
		// @formatter:off
		var rendered = node switch
		{
			MfmCenterNode mfmCenterNode         => MfmCenterNode(mfmCenterNode, document),
			MfmCodeBlockNode mfmCodeBlockNode   => MfmCodeBlockNode(mfmCodeBlockNode, document),
			MfmMathBlockNode mfmMathBlockNode   => throw new NotImplementedException($"{mfmMathBlockNode.GetType()}"),
			MfmQuoteNode mfmQuoteNode           => MfmQuoteNode(mfmQuoteNode, document),
			IMfmBlockNode mfmBlockNode           => throw new NotImplementedException($"{mfmBlockNode.GetType()}"),
			MfmBoldNode mfmBoldNode             => MfmBoldNode(mfmBoldNode, document),
			MfmEmojiCodeNode mfmEmojiCodeNode   => MfmEmojiCodeNode(mfmEmojiCodeNode, document, emoji, simple),
			MfmFnNode mfmFnNode                 => MfmFnNode(mfmFnNode, document),
			MfmHashtagNode mfmHashtagNode       => MfmHashtagNode(mfmHashtagNode, document),
			MfmInlineCodeNode mfmInlineCodeNode => MfmInlineCodeNode(mfmInlineCodeNode, document),
			MfmItalicNode mfmItalicNode         => MfmItalicNode(mfmItalicNode, document),
			MfmLinkNode mfmLinkNode             => MfmLinkNode(mfmLinkNode, document),
			MfmInlineMathNode mfmInlineMathNode => throw new NotImplementedException($"{mfmInlineMathNode.GetType()}"),
			MfmMentionNode mfmMentionNode       => MfmMentionNode(mfmMentionNode, document, accountDomain),
			MfmPlainNode mfmPlainNode           => MfmPlainNode(mfmPlainNode, document),
			MfmSmallNode mfmSmallNode           => MfmSmallNode(mfmSmallNode, document),
			MfmStrikeNode mfmStrikeNode         => MfmStrikeNode(mfmStrikeNode, document),
			MfmTextNode mfmTextNode             => MfmTextNode(mfmTextNode, document),
			MfmUrlNode mfmUrlNode               => MfmUrlNode(mfmUrlNode, document),
			IMfmInlineNode mfmInlineNode         => throw new NotImplementedException($"{mfmInlineNode.GetType()}"),
			_ => throw new ArgumentOutOfRangeException(nameof(node))
		};
		// @formatter:on

		if (node.Children.Length > 0 && rendered.ChildNodes.Length == 0)
		{
			foreach (var childNode in node.Children)
			{
				try
				{
					rendered.AppendNodes(RenderNode(childNode, document, emoji, accountDomain, simple));
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

	private static INode MfmPlainNode(MfmPlainNode _, IDocument document)
	{
		var el = document.CreateElement("span");
		el.ClassName = "plain";
		return el;
	}

	private static INode MfmCenterNode(MfmCenterNode _, IDocument document)
	{
		var el = document.CreateElement("div");
		el.SetAttribute("style", "text-align: center");
		return el;
	}

	private static INode MfmCodeBlockNode(MfmCodeBlockNode node, IDocument document)
	{
		var el = document.CreateElement("pre");
		el.ClassName = "code-pre";
		var childEl = document.CreateElement("code");
		childEl.TextContent = node.Code;
		el.AppendChild(childEl);
		return el;
	}

	private static INode MfmQuoteNode(MfmQuoteNode _, IDocument document)
	{
		var el = document.CreateElement("blockquote");
		el.ClassName = "quote-node";
		return el;
	}

	private static INode MfmInlineCodeNode(MfmInlineCodeNode node, IDocument document)
	{
		var el = document.CreateElement("code");
		el.TextContent = node.Code;
		return el;
	}

	private static INode MfmHashtagNode(MfmHashtagNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", $"/tags/{node.Hashtag}");
		el.ClassName   = "hashtag-node";
		el.TextContent = "#" + node.Hashtag;
		return el;
	}

	private static INode MfmLinkNode(MfmLinkNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", node.Url);
		el.SetAttribute("target", "_blank");
		el.ClassName = "link-node";
		return el;
	}

	private static INode MfmItalicNode(MfmItalicNode _, IDocument document)
	{
		var el = document.CreateElement("span");
		el.SetAttribute("style", "font-style: italic");
		return el;
	}

	private static INode MfmEmojiCodeNode(
		MfmEmojiCodeNode node, IDocument document, List<EmojiResponse> emojiList, bool simple
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

	private static INode MfmUrlNode(MfmUrlNode node, IDocument document)
	{
		var el = document.CreateElement("a");
		el.SetAttribute("href", node.Url);
		el.SetAttribute("target", "_blank");
		el.ClassName   = "url-node";
		el.TextContent = node.Url;
		return el;
	}

	private static INode MfmBoldNode(MfmBoldNode _, IDocument document)
	{
		var el = document.CreateElement("strong");
		return el;
	}

	private static INode MfmSmallNode(MfmSmallNode _, IDocument document)
	{
		var el = document.CreateElement("small");
		el.SetAttribute("style", "opacity: 0.7;");
		return el;
	}

	private static INode MfmStrikeNode(MfmStrikeNode _, IDocument document)
	{
		var el = document.CreateElement("del");
		return el;
	}

	private static INode MfmTextNode(MfmTextNode node, IDocument document)
	{
		var el = document.CreateElement("span");
		el.TextContent = node.Text;
		return el;
	}

	private static INode MfmMentionNode(MfmMentionNode node, IDocument document, string accountDomain)
	{
		var link = document.CreateElement("a");
		link.SetAttribute("href",
		                  node.Host != null && node.Host != accountDomain
			                  ? $"/@{node.Acct}"
			                  : $"/@{node.User}");
		link.ClassName = "mention";
		var userPart = document.CreateElement("span");
		userPart.ClassName   = "user";
		userPart.TextContent = $"@{node.User}";
		link.AppendChild(userPart);
		if (node.Host != null && node.Host != accountDomain)
		{
			var hostPart = document.CreateElement("span");
			hostPart.ClassName   = "host";
			hostPart.TextContent = $"@{node.Host}";
			link.AppendChild(hostPart);
		}

		return link;
	}

	private static INode MfmFnNode(MfmFnNode node, IDocument document)
	{
		// Simplify node.Args structure to make it more readable in below functions
		var args = node.Args ?? [];
		
		return node.Name switch {
			"flip"     => MfmFnFlip(args, document),
			"font"     => MfmFnFont(args, document),
			"x2"       => MfmFnX(node.Name, document),
			"x3"       => MfmFnX(node.Name, document),
			"x4"       => MfmFnX(node.Name, document),
			"blur"     => MfmFnBlur(document),
			"jelly"    => MfmFnAnimation(node.Name, args, document),
			"tada"     => MfmFnAnimation(node.Name, args, document),
			"jump"     => MfmFnAnimation(node.Name, args, document, "0.75s"),
			"bounce"   => MfmFnAnimation(node.Name, args, document, "0.75s"),
			"spin"     => MfmFnSpin(args, document),
			"shake"    => MfmFnAnimation(node.Name, args, document, "0.5s"),
			"twitch"   => MfmFnAnimation(node.Name, args, document, "0.5s"),
			"rainbow"  => MfmFnAnimation(node.Name, args, document),
			"sparkle"  => throw new NotImplementedException($"{node.Name}"),
			"rotate"   => MfmFnRotate(args, document),
			"fade"     => MfmFnFade(args, document),
			"crop"     => MfmFnCrop(args, document),
			"position" => MfmFnPosition(args, document),
			"scale"    => MfmFnScale(args, document),
			"fg"       => MfmFnFg(args, document),
			"bg"       => MfmFnBg(args, document),
			"border"   => MfmFnBorder(args, document),
			"ruby"     => MfmFnRuby(node, document),
			"unixtime" => MfmFnUnixtime(node, document),
			_          => throw new NotImplementedException($"{node.Name}")
		};
	}

	private static INode MfmFnFlip(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		if (args.ContainsKey("h") && args.ContainsKey("v"))
			el.ClassName = "fn-flip h v";
		else if (args.ContainsKey("v"))
			el.ClassName = "fn-flip v";
		else
			el.ClassName = "fn-flip h";

		return el;
	}
	
	private static INode MfmFnFont(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		if (args.ContainsKey("serif"))
			el.SetAttribute("style", "font-family: serif;");
		else if (args.ContainsKey("monospace"))
			el.SetAttribute("style", "font-family: monospace;");
		else if (args.ContainsKey("cursive"))
			el.SetAttribute("style", "font-family: cursive;");
		else if (args.ContainsKey("fantasy"))
			el.SetAttribute("style", "font-family: fantasy;");

		return el;
	}

	private static INode MfmFnX(string name, IDocument document)
	{
		var el = document.CreateElement("span");

		el.SetAttribute("style", $"display: inline-block; font-size: {name.Replace("x", "")}em;");

		return el;
	}
	
	private static INode MfmFnBlur(IDocument document)
	{
		var el = document.CreateElement("span");

		el.ClassName = "fn-blur";

		return el;
	}

	private static INode MfmFnAnimation(
		string name, Dictionary<string, string?> args, IDocument document, string defaultSpeed = "1s"
	)
	{
		var el = document.CreateElement("span");

		el.ClassName = "fn-animation";

		var style = $"animation-name: fn-{name}-mfm;";
		style += args.TryGetValue("speed", out var speed)
			? $" animation-duration: {speed};"
			: $" animation-duration: {defaultSpeed};";
		style += args.TryGetValue("delay", out var delay) ? $" animation-delay: {delay};" : "";
		style += args.TryGetValue("loop", out var loop) ? $" animation-iteration-count: {loop};" : "";

		el.SetAttribute("style", style.Trim());

		return el;
	}
	
	private static INode MfmFnSpin(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		el.ClassName = "fn-spin";

		var name = args.ContainsKey("y")
			? "fn-spin-y-mfm"
			: args.ContainsKey("x")
				? "fn-spin-x-mfm"
				: "fn-spin-mfm";
		var direction = args.ContainsKey("alternate")
			? "alternate"
			: args.ContainsKey("left")
				? "reverse"
				: "normal";

		var style = $"animation-name: {name}; animation-direction: {direction};";
		style += args.TryGetValue("speed", out var speed) ? $" animation-duration: {speed};" : "";
		style += args.TryGetValue("delay", out var delay) ? $" animation-delay: {delay};" : "";
		style += args.TryGetValue("loop", out var loop) ? $" animation-iteration-count: {loop};" : "";

		el.SetAttribute("style", style.Trim());

		return el;
	}

	private static INode MfmFnRotate(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var deg = args.GetValueOrDefault("deg") ?? "90";

		el.ClassName = "fn-rotate";
		if (args.ContainsKey("x"))
			el.SetAttribute("style", $"transform: perspective(120px) rotateX({deg}deg);");
		else
			el.SetAttribute("style", $"transform: rotate({deg}deg);");

		return el;
	}
	
	private static INode MfmFnFade(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		el.ClassName = "fn-fade";

		var style = "";
		style += args.ContainsKey("out") ? "animation-direction: alternate-reverse; " : "";
		style += args.TryGetValue("speed", out var speed) ? $"animation-duration: {speed}; " : "";
		style += args.TryGetValue("delay", out var delay) ? $"animation-delay: {delay}; " : "";

		if (!string.IsNullOrWhiteSpace(style))
			el.SetAttribute("style", style.Trim());

		return el;
	}

	private static INode MfmFnCrop(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var inset = $"{args.GetValueOrDefault("top") ?? "0"}% {args.GetValueOrDefault("right") ?? "0"}% {args.GetValueOrDefault("bottom") ?? "0"}% {args.GetValueOrDefault("left") ?? "0"}%";
		el.SetAttribute("style", $"display: inline-block; clip-path: inset({inset});");

		return el;
	}
	
	private static INode MfmFnPosition(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var translateX = args.GetValueOrDefault("x") ?? "0";
		var translateY = args.GetValueOrDefault("y") ?? "0";
		el.SetAttribute("style", $"display: inline-block; transform: translateX({translateX}em) translateY({translateY}em);");

		return el;
	}
	
	private static INode MfmFnScale(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var scaleX = args.GetValueOrDefault("x") ?? "1";
		var scaleY = args.GetValueOrDefault("y") ?? "1";
		el.SetAttribute("style", $"display: inline-block; transform: scale({scaleX}, {scaleY});");

		return el;
	}

	[GeneratedRegex(@"^[0-9a-f]{3,6}$", RegexOptions.IgnoreCase)]
	private static partial Regex ColorRegex();
	
	private static bool ValidColor(string? color)
	{
		return color != null && ColorRegex().Match(color).Success;
	}
	
	private static INode MfmFnFg(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		if (args.TryGetValue("color", out var color) && ValidColor(color))
			el.SetAttribute("style", $"display: inline-block; color: #{color};");

		return el;
	}
	
	private static INode MfmFnBg(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		if (args.TryGetValue("color", out var color) && ValidColor(color))
			el.SetAttribute("style", $"display: inline-block; background-color: #{color};");

		return el;
	}
	
	private static INode MfmFnBorder(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var width  = args.GetValueOrDefault("width") ?? "1";
		var radius = args.GetValueOrDefault("radius") ?? "0";
		var style  = args.GetValueOrDefault("style") ?? "solid";
		var color  = args.TryGetValue("color", out var c) && ValidColor(c) ? "#" + c : "var(--notice-color)";
		
		el.SetAttribute("style", $"display: inline-block; border: {width}px {style} {color}; border-radius: {radius}px; overflow: clip;");

		return el;
	}

	private static string? GetNodeText(IMfmNode node)
	{
		return node switch
		{
			MfmTextNode mfmTextNode => mfmTextNode.Text,
			_                                    => null,
		};
	}

	private static INode MfmFnRuby(MfmFnNode node, IDocument document)
	{
		var el = document.CreateElement("ruby");

		if (node.Children.Length != 1) return el;
		var childText = GetNodeText(node.Children[0]);
		if (childText == null) return el;
		var split = childText.SplitSpaces();
		if (split.Length < 2) return el;
		
		el.TextContent = split[0];

		var rp1 = document.CreateElement("rp");
		rp1.TextContent = "(";
		el.AppendChild(rp1);

		var rt = document.CreateElement("rt");
		rt.TextContent = split[1];
		el.AppendChild(rt);

		var rp2 = document.CreateElement("rp");
		rp1.TextContent = ")";
		el.AppendChild(rp2);

		return el;
	}

	private static INode MfmFnUnixtime(MfmFnNode node, IDocument document)
	{
		var el = document.CreateElement("time");

		if (node.Children.Length != 1) return el;
		var childText = GetNodeText(node.Children[0]);
		if (childText == null) return el;

		double timestamp;
		try
		{
			timestamp = double.Parse(childText);
		}
		catch
		{
			return el;
		}

		var date = DateTime.UnixEpoch.AddSeconds(timestamp);
		el.SetAttribute("datetime", date.ToString("O"));
		el.TextContent = date.ToString("G");

		return el;
	}
}