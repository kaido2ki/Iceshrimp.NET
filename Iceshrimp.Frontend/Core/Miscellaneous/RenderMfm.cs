using AngleSharp;
using AngleSharp.Dom;
using Iceshrimp.Parsing;
using Microsoft.AspNetCore.Components;

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
            catch (NotImplementedException)
            {
                var fallback = document.CreateElement("span");
                fallback.TextContent = "Node type not implemented";
                el.AppendNodes(fallback);
            }
        }

        return el;
    }
    private static INode RenderNode(MfmNodeTypes.MfmNode node, IDocument document)
    {
        var rendered = node switch
             {
                 MfmNodeTypes.MfmCenterNode mfmCenterNode         => throw new NotImplementedException(),
                 MfmNodeTypes.MfmCodeBlockNode mfmCodeBlockNode   => throw new NotImplementedException(),
                 MfmNodeTypes.MfmMathBlockNode mfmMathBlockNode   => throw new NotImplementedException(),
                 MfmNodeTypes.MfmQuoteNode mfmQuoteNode           => throw new NotImplementedException(),
                 MfmNodeTypes.MfmSearchNode mfmSearchNode         => throw new NotImplementedException(),
                 MfmNodeTypes.MfmBlockNode mfmBlockNode           => throw new NotImplementedException(),
                 MfmNodeTypes.MfmBoldNode mfmBoldNode             => MfmBoldNode(mfmBoldNode, document),
                 MfmNodeTypes.MfmEmojiCodeNode mfmEmojiCodeNode   => MfmEmojiCodeNode(mfmEmojiCodeNode, document),
                 MfmNodeTypes.MfmFnNode mfmFnNode                 => throw new NotImplementedException(),
                 MfmNodeTypes.MfmHashtagNode mfmHashtagNode       => throw new NotImplementedException(),
                 MfmNodeTypes.MfmInlineCodeNode mfmInlineCodeNode => throw new NotImplementedException(),
                 MfmNodeTypes.MfmItalicNode mfmItalicNode         => MfmItalicNode(mfmItalicNode, document),
                 MfmNodeTypes.MfmLinkNode mfmLinkNode             => throw new NotImplementedException(),
                 MfmNodeTypes.MfmMathInlineNode mfmMathInlineNode => throw new NotImplementedException(),
                 MfmNodeTypes.MfmMentionNode mfmMentionNode       => MfmMentionNode(mfmMentionNode, document),
                 MfmNodeTypes.MfmPlainNode mfmPlainNode           => throw new NotImplementedException(),
                 MfmNodeTypes.MfmSmallNode mfmSmallNode           => throw new NotImplementedException(),
                 MfmNodeTypes.MfmStrikeNode mfmStrikeNode         => throw new NotImplementedException(),
                 MfmNodeTypes.MfmTextNode mfmTextNode             => MfmTextNode(mfmTextNode, document),
                 MfmNodeTypes.MfmUrlNode mfmUrlNode               => MfmUrlNode(mfmUrlNode, document),
                 MfmNodeTypes.MfmInlineNode mfmInlineNode         => throw new NotImplementedException(),
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
                catch (NotImplementedException)
                {
                    var fallback = document.CreateElement("span");
                    fallback.TextContent = "Node type not implemented";
                    rendered.AppendNodes(fallback);
                }
            }
        }
        return rendered;
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