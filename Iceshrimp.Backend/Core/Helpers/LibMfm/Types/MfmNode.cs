namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Types;

public abstract class MfmNode
{
	public IEnumerable<MfmNode> Children = [];
}

public abstract class MfmInlineNode : MfmNode
{
	public new IEnumerable<MfmInlineNode> Children
	{
		set => base.Children = value;
	}
}

public abstract class MfmBlockNode : MfmNode
{
	public new IEnumerable<MfmInlineNode> Children
	{
		set => base.Children = value;
	}
}

public abstract class MfmPureInlineNode : MfmInlineNode
{
	public new required IEnumerable<MfmInlineNode> Children
	{
		set => base.Children = value;
	}
}

public abstract class MfmPureBlockNode : MfmNode
{
	public new required IEnumerable<MfmInlineNode> Children
	{
		set => base.Children = value;
	}
}

public sealed class MfmQuoteNode : MfmPureBlockNode;

public sealed class MfmSearchNode : MfmBlockNode
{
	public required string Content;
	public required string Query;
}

public sealed class MfmCodeBlockNode : MfmBlockNode
{
	public required string  Code;
	public required string? Language;
}

public sealed class MfmMathBlockNode : MfmBlockNode
{
	public required string Formula;
}

public sealed class MfmCenterNode : MfmPureBlockNode;

public sealed class MfmUnicodeEmojiNode : MfmInlineNode
{
	public required string Emoji;
}

public sealed class MfmEmojiCodeNode : MfmInlineNode
{
	public required string Name;
}

public sealed class MfmBoldNode : MfmPureInlineNode;

public sealed class MfmSmallNode : MfmPureInlineNode;

public sealed class MfmItalicNode : MfmPureInlineNode;

public sealed class MfmStrikeNode : MfmPureInlineNode;

public sealed class MfmInlineCodeNode : MfmInlineNode
{
	public required string Code;
}

public sealed class MfmMathInlineNode : MfmInlineNode
{
	public required string Formula;
}

public sealed class MfmMentionNode : MfmInlineNode
{
	public required string  Acct;
	public required string? Host;
	public required string  Username;
}

public sealed class MfmHashtagNode : MfmInlineNode
{
	public required string Hashtag;
}

public sealed class MfmUrlNode : MfmInlineNode
{
	public required bool   Brackets;
	public required string Url;
}

public sealed class MfmLinkNode : MfmPureInlineNode
{
	public required bool   Silent;
	public required string Url;
}

public sealed class MfmFnNode : MfmPureInlineNode
{
	public required Dictionary<string, string> Args;

	public required string Name;
	//TODO: implement (string, bool) args
}

public sealed class MfmPlainNode : MfmInlineNode
{
	public new required IEnumerable<MfmTextNode> Children
	{
		set => base.Children = value;
	}
}

public sealed class MfmTextNode : MfmInlineNode
{
	public required string Text;
}