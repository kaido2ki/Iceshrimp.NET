using AngleSharp;
using AngleSharp.Dom;
using Iceshrimp.Parsing;
using Microsoft.AspNetCore.Components;
using Microsoft.FSharp.Core;

namespace Iceshrimp.Frontend.Core.Miscellaneous;

public class MfmRenderer
{
    public static async Task<MarkupString> RenderString(string text)
    {
        var res         = Mfm.parse(text);
        var context     = BrowsingContext.New();
        var document    = await context.OpenNewAsync();
        var renderedMfm = MfmRenderer.RenderMultipleNodes(res, document);
        var html        = renderedMfm.ToHtml();
        return new MarkupString(html);
    }
    public static INode RenderMultipleNodes(IEnumerable<MfmNodeTypes.MfmNode> nodes, IDocument document)
    {
        var el = document.CreateElement("p");
        el.SetAttribute("mfm", "mfm");
        foreach (var node in nodes)
        {
            try
            {
                el.AppendNodes(RenderNode(node, document));
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
    private static INode RenderNode(MfmNodeTypes.MfmNode node, IDocument document)
    {
        var rendered = node switch
             {
                 MfmNodeTypes.MfmCenterNode mfmCenterNode         => throw new NotImplementedException($"{mfmCenterNode.GetType()}"),
                 MfmNodeTypes.MfmCodeBlockNode mfmCodeBlockNode   => MfmCodeBlockNode(mfmCodeBlockNode, document),
                 MfmNodeTypes.MfmMathBlockNode mfmMathBlockNode   => throw new NotImplementedException($"{mfmMathBlockNode.GetType()}"),
                 MfmNodeTypes.MfmQuoteNode mfmQuoteNode           => throw new NotImplementedException($"{mfmQuoteNode.GetType()}"),
                 MfmNodeTypes.MfmSearchNode mfmSearchNode         => throw new NotImplementedException($"{mfmSearchNode.GetType()}"),
                 MfmNodeTypes.MfmBlockNode mfmBlockNode           => throw new NotImplementedException($"{mfmBlockNode.GetType()}"),
                 MfmNodeTypes.MfmBoldNode mfmBoldNode             => MfmBoldNode(mfmBoldNode, document),
                 MfmNodeTypes.MfmEmojiCodeNode mfmEmojiCodeNode   => MfmEmojiCodeNode(mfmEmojiCodeNode, document),
                 MfmNodeTypes.MfmFnNode mfmFnNode                 => throw new NotImplementedException($"{mfmFnNode.GetType()}"),
                 MfmNodeTypes.MfmHashtagNode mfmHashtagNode       => throw new NotImplementedException($"{mfmHashtagNode.GetType()}"),
                 MfmNodeTypes.MfmInlineCodeNode mfmInlineCodeNode => MfmInlineCodeNode(mfmInlineCodeNode, document),
                 MfmNodeTypes.MfmItalicNode mfmItalicNode         => MfmItalicNode(mfmItalicNode, document),
                 MfmNodeTypes.MfmLinkNode mfmLinkNode             => MfmLinkNode(mfmLinkNode, document),
                 MfmNodeTypes.MfmMathInlineNode mfmMathInlineNode => throw new NotImplementedException($"{mfmMathInlineNode.GetType()}"),
                 MfmNodeTypes.MfmMentionNode mfmMentionNode       => MfmMentionNode(mfmMentionNode, document),
                 MfmNodeTypes.MfmPlainNode mfmPlainNode           => throw new NotImplementedException($"{mfmPlainNode.GetType()}"),
                 MfmNodeTypes.MfmSmallNode mfmSmallNode           => throw new NotImplementedException($"{mfmSmallNode.GetType()}"),
                 MfmNodeTypes.MfmStrikeNode mfmStrikeNode         => throw new NotImplementedException($"{mfmStrikeNode.GetType()}"),
                 MfmNodeTypes.MfmTextNode mfmTextNode             => MfmTextNode(mfmTextNode, document),
                 MfmNodeTypes.MfmUrlNode mfmUrlNode               => MfmUrlNode(mfmUrlNode, document),
                 MfmNodeTypes.MfmInlineNode mfmInlineNode         => throw new NotImplementedException($"{mfmInlineNode.GetType()}"),
                 _                                                => throw new ArgumentOutOfRangeException(nameof(node))
             };
        if (node.Children.Length > 0)
        {
            foreach (var childNode in node.Children)
            {
                try
                {
                    rendered.AppendNodes(RenderNode(childNode, document));
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

    private static INode MfmCodeBlockNode(MfmNodeTypes.MfmCodeBlockNode node, IDocument document)
    {
        var el      = document.CreateElement("pre");
        var childEl = document.CreateElement("code");
        childEl.TextContent = node.Code;
        el.AppendChild(childEl);
        return el;
    }

    private static INode MfmInlineCodeNode(MfmNodeTypes.MfmInlineCodeNode node, IDocument document)
    {
        var el = document.CreateElement("code");
        el.TextContent = node.Code;
        return el;
    }

    private static INode MfmLinkNode(MfmNodeTypes.MfmLinkNode node, IDocument document)
    {
        var el = document.CreateElement("a");
        el.SetAttribute("href", node.Url);
        el.ClassName   = "link-node";
        return el;
    }

    private static INode MfmItalicNode(MfmNodeTypes.MfmItalicNode node, IDocument document)
    {
        var el = document.CreateElement("span");
        el.SetAttribute("style", "font-style: italic");
        return el;
    }

    private static INode MfmEmojiCodeNode(MfmNodeTypes.MfmEmojiCodeNode node, IDocument document)
    {
        var el = document.CreateElement("span");
        el.TextContent = node.Name;
        el.ClassName   = "emoji";
        return el;
    }
    private static INode MfmUrlNode(MfmNodeTypes.MfmUrlNode node, IDocument document)
    {
        var el = document.CreateElement("a");
        el.SetAttribute("href", node.Url);
        el.ClassName   = "url-node";
        el.TextContent = node.Url;
        return el;
    }

    private static INode MfmBoldNode(MfmNodeTypes.MfmBoldNode node, IDocument document)
    {
        var el = document.CreateElement("strong");
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