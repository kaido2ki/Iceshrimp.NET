using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Iceshrimp.Parsing;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.FSharp.Core;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public static partial class MfmRenderer
{
	public static async Task<MarkupString> RenderStringAsync(
		string text, List<EmojiResponse> emoji, string accountDomain, bool simple = false
	)
	{
		var res         = simple ? Mfm.parseSimple(text) : Mfm.parse(text);
		var context     = BrowsingContext.New();
		var document    = await context.OpenNewAsync();
		var renderedMfm = RenderMultipleNodes(res, document, emoji, accountDomain, simple);
		var html        = renderedMfm.ToHtml();
		return new MarkupString(html);
	}

	private static INode RenderMultipleNodes(
		IEnumerable<MfmNodeTypes.MfmNode> nodes, IDocument document, List<EmojiResponse> emoji, string accountDomain, bool simple
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
		MfmNodeTypes.MfmNode node, IDocument document, List<EmojiResponse> emoji, string accountDomain, bool simple
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
			MfmNodeTypes.MfmFnNode mfmFnNode                 => MfmFnNode(mfmFnNode, document),
			MfmNodeTypes.MfmHashtagNode mfmHashtagNode       => MfmHashtagNode(mfmHashtagNode, document),
			MfmNodeTypes.MfmInlineCodeNode mfmInlineCodeNode => MfmInlineCodeNode(mfmInlineCodeNode, document),
			MfmNodeTypes.MfmItalicNode mfmItalicNode         => MfmItalicNode(mfmItalicNode, document),
			MfmNodeTypes.MfmLinkNode mfmLinkNode             => MfmLinkNode(mfmLinkNode, document),
			MfmNodeTypes.MfmMathInlineNode mfmMathInlineNode => throw new NotImplementedException($"{mfmMathInlineNode.GetType()}"),
			MfmNodeTypes.MfmMentionNode mfmMentionNode       => MfmMentionNode(mfmMentionNode, document, accountDomain),
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

	private static INode MfmMentionNode(MfmNodeTypes.MfmMentionNode node, IDocument document, string accountDomain)
	{
		var link = document.CreateElement("a");
		link.SetAttribute("href",
		                  node.Host != null && node.Host.Value != accountDomain
			                  ? $"/@{node.Acct}"
			                  : $"/@{node.Username}");
		link.ClassName = "mention";
		var userPart = document.CreateElement("span");
		userPart.ClassName   = "user";
		userPart.TextContent = $"@{node.Username}";
		link.AppendChild(userPart);
		if (node.Host != null && node.Host.Value != accountDomain)
		{
			var hostPart = document.CreateElement("span");
			hostPart.ClassName   = "host";
			hostPart.TextContent = $"@{node.Host.Value}";
			link.AppendChild(hostPart);
		}

		return link;
	}

	private static INode MfmFnNode(MfmNodeTypes.MfmFnNode node, IDocument document)
	{
		// FSharpOption is a pain to work with in C#, this makes dealing with the args a lot easier
		var args = FSharpOption<IDictionary<string, FSharpOption<string>>>.get_IsSome(node.Args)
			? node.Args.Value.ToDictionary(p => p.Key,
			                               p => FSharpOption<string>.get_IsSome(p.Value) ? p.Value.Value : null)
			: new Dictionary<string, string?>();

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
			"shake"    => throw new NotImplementedException($"{node.Name}"),
			"twitch"   => throw new NotImplementedException($"{node.Name}"),
			"rainbow"  => throw new NotImplementedException($"{node.Name}"),
			"sparkle"  => throw new NotImplementedException($"{node.Name}"),
			"rotate"   => MfmFnRotate(args, document),
			"fade"     => throw new NotImplementedException($"{node.Name}"),
			"crop"     => MfmFnCrop(args, document),
			"position" => MfmFnPosition(args, document),
			"scale"    => MfmFnScale(args, document),
			"fg"       => MfmFnFg(args, document),
			"bg"       => MfmFnBg(args, document),
			"border"   => MfmFnBorder(args, document),
			"ruby"     => throw new NotImplementedException($"{node.Name}"),
			"unixtime" => throw new NotImplementedException($"{node.Name}"),
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
			el.SetAttribute("style", $"color: #{color};");

		return el;
	}
	
	private static INode MfmFnBg(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		if (args.TryGetValue("color", out var color) && ValidColor(color))
			el.SetAttribute("style", $"background-color: #{color};");

		return el;
	}
	
	private static INode MfmFnBorder(Dictionary<string, string?> args, IDocument document)
	{
		var el = document.CreateElement("span");

		var width  = args.GetValueOrDefault("width") ?? "1";
		var radius = args.GetValueOrDefault("radius") ?? "0";
		var style  = args.GetValueOrDefault("style") ?? "solid";
		var color  = args.TryGetValue("color", out var c) && ValidColor(c) ? "#" + c : "var(--notice-color)";
		
		el.SetAttribute("style", $"display: inline-block; border: {width}px {style} {color}; border-radius: {radius}px;");

		return el;
	}
}