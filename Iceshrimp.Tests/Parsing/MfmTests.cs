using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Iceshrimp.Parsing;
using static Iceshrimp.Parsing.MfmNodeTypes;
using FSDict = System.Collections.Generic.Dictionary<string, Microsoft.FSharp.Core.FSharpOption<string>?>;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class MfmTests
{
	[TestMethod]
	public void TestParseBoldItalic()
	{
		// @formatter:off
		List<MfmNode> expected123 =
		[
			new MfmItalicNode([
				new MfmTextNode("italic "),
				new MfmBoldNode([new MfmTextNode("bold")], InlineNodeType.Symbol),
				new MfmTextNode(" italic")
			], InlineNodeType.Symbol)
		];
		
		List<MfmNode> expected4 =
		[
			new MfmItalicNode([
				new MfmTextNode("italic "),
				new MfmBoldNode([new MfmTextNode("bold")], InlineNodeType.HtmlTag),
				new MfmTextNode(" italic")
			], InlineNodeType.HtmlTag)
		];
		
		List<MfmNode> expected5 =
		[
			new MfmItalicNode([
				new MfmTextNode("italic "),
				new MfmBoldNode([new MfmTextNode("bold")], InlineNodeType.Symbol),
				new MfmTextNode(" italic")
			], InlineNodeType.HtmlTag)
		];
		
		List<MfmNode> expected6 =
		[
			new MfmItalicNode([
				new MfmTextNode("italic "),
				new MfmBoldNode([new MfmTextNode("bold")], InlineNodeType.HtmlTag),
				new MfmTextNode(" italic")
			], InlineNodeType.Symbol)
		];
		// @formatter:on

		const string input  = "*italic **bold** italic*";
		const string input2 = "_italic **bold** italic_";
		const string input3 = "_italic __bold__ italic_";
		const string input4 = "<i>italic <b>bold</b> italic</i>";
		const string input5 = "<i>italic **bold** italic</i>";
		const string input6 = "*italic <b>bold</b> italic*";

		var res  = Mfm.parse(input).ToList();
		var res2 = Mfm.parse(input2).ToList();
		var res3 = Mfm.parse(input3).ToList();
		var res4 = Mfm.parse(input4).ToList();
		var res5 = Mfm.parse(input5).ToList();
		var res6 = Mfm.parse(input6).ToList();

		AssertionOptions.FormattingOptions.MaxDepth = 100;

		res.Should().Equal(expected123, MfmNodeEqual);
		res2.Should().Equal(expected123, MfmNodeEqual);
		res3.Should().Equal(expected123, MfmNodeEqual);
		res4.Should().Equal(expected4, MfmNodeEqual);
		res5.Should().Equal(expected5, MfmNodeEqual);
		res6.Should().Equal(expected6, MfmNodeEqual);

		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
		MfmSerializer.Serialize(res2).Should().BeEquivalentTo(input);
		MfmSerializer.Serialize(res3).Should().BeEquivalentTo(input);
		MfmSerializer.Serialize(res4).Should().BeEquivalentTo(input4);
		MfmSerializer.Serialize(res5).Should().BeEquivalentTo(input5);
		MfmSerializer.Serialize(res6).Should().BeEquivalentTo(input6);
	}

	[TestMethod]
	public void TestItalicNegative()
	{
		List<MfmNode> expected    = [new MfmTextNode("test*test*test")];
		List<MfmNode> expectedAlt = [new MfmTextNode("test_test_test")];

		Mfm.parse("test*test*test").ToList().Should().Equal(expected, MfmNodeEqual);
		Mfm.parse("test_test_test").ToList().Should().Equal(expectedAlt, MfmNodeEqual);

		expected    = [new MfmTextNode("test*test* test")];
		expectedAlt = [new MfmTextNode("test_test_ test")];

		Mfm.parse("test*test* test").ToList().Should().Equal(expected, MfmNodeEqual);
		Mfm.parse("test_test_ test").ToList().Should().Equal(expectedAlt, MfmNodeEqual);

		expected =
		[
			new MfmTextNode("test "),
			new MfmItalicNode([new MfmTextNode("test")], InlineNodeType.Symbol),
			new MfmTextNode("test")
		];

		Mfm.parse("test *test*test").ToList().Should().Equal(expected, MfmNodeEqual);
		Mfm.parse("test _test_test").ToList().Should().Equal(expected, MfmNodeEqual);
	}

	[TestMethod]
	public void TestStrike()
	{
		const string input  = "~~test~~";
		const string input2 = "<s>test</s>";

		List<MfmNode> expected  = [new MfmStrikeNode([new MfmTextNode("test")], InlineNodeType.Symbol)];
		List<MfmNode> expected2 = [new MfmStrikeNode([new MfmTextNode("test")], InlineNodeType.HtmlTag)];

		var res  = Mfm.parse(input).ToList();
		var res2 = Mfm.parse(input2).ToList();

		res.Should().Equal(expected, MfmNodeEqual);
		res2.Should().Equal(expected2, MfmNodeEqual);

		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
		MfmSerializer.Serialize(res2).Should().BeEquivalentTo(input2);
	}

	[TestMethod]
	public void TestParseList()
	{
		const string input = """
		                     * test
		                     * test2
		                     * test3
		                     """;

		List<MfmNode> expected = [new MfmTextNode("* test\n* test2\n* test3")];

		var res = Mfm.parse(input).ToList();
		res.Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestParseCode()
	{
		List<MfmNode> expected =
		[
			new MfmInlineCodeNode("test"), new MfmCodeBlockNode("test", null), new MfmCodeBlockNode("test", "lang")
		];

		var res = Mfm.parse("""
		                    `test`
		                    ```
		                    test
		                    ```
		                    ```lang
		                    test
		                    ```
		                    """);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
	}

	[TestMethod]
	public void TestWhitespaceAtSign()
	{
		const string  input    = "test @ test";
		List<MfmNode> expected = [new MfmTextNode(input)];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestMention()
	{
		const string input =
			"test @test test @test@instance.tld @test_ @_test @test_@ins-tance.tld @_test@xn--mastodn-f1a.de @_test@-xn--mastodn-f1a.de (@test@domain.tld)";
		List<MfmNode> expected =
		[
			new MfmTextNode("test "),
			new MfmMentionNode("test", "test", null),
			new MfmTextNode(" test "),
			new MfmMentionNode("test@instance.tld", "test", "instance.tld"),
			new MfmTextNode(" "),
			new MfmMentionNode("test_", "test_", null),
			new MfmTextNode(" "),
			new MfmMentionNode("_test", "_test", null),
			new MfmTextNode(" "),
			new MfmMentionNode("test_@ins-tance.tld", "test_", "ins-tance.tld"),
			new MfmTextNode(" "),
			new MfmMentionNode("_test@xn--mastodn-f1a.de", "_test", "xn--mastodn-f1a.de"),
			new MfmTextNode(" @_test@-xn--mastodn-f1a.de ("),
			new MfmMentionNode("test@domain.tld", "test", "domain.tld"),
			new MfmTextNode(")"),
		];
		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestInvalidMention()
	{
		const string  input    = "test @test@ test";
		List<MfmNode> expected = [new MfmTextNode("test @test@ test")];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestMentionTrailingDot()
	{
		const string  input    = "@test@asdf.com.";
		List<MfmNode> expected = [new MfmMentionNode("test@asdf.com", "test", "asdf.com"), new MfmTextNode(".")];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestMentionTrailingDotLocal()
	{
		const string  input    = "@test.";
		List<MfmNode> expected = [new MfmMentionNode("test", "test", null), new MfmTextNode(".")];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestCodeBlock()
	{
		const string canonical = """
		                         test 123
		                         ```
		                         this is a code block
		                         ```
		                         test 123
		                         """;

		const string alt = """
		                   test 123

		                   ```
		                   this is a code block
		                   ```

		                   test 123
		                   """;

		List<MfmNode> expected =
		[
			new MfmTextNode("test 123"),
			new MfmCodeBlockNode("this is a code block", null),
			new MfmTextNode("test 123")
		];
		var res  = Mfm.parse(canonical);
		var res2 = Mfm.parse(alt);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		res2.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(canonical);
		MfmSerializer.Serialize(res2).Should().BeEquivalentTo(canonical);
	}

	[TestMethod]
	public void TestCodeBlockMultiLine()
	{
		const string input = """
		                     ```cs
		                     asd
		                     sdf
		                     ```
		                     """;

		List<MfmNode> expected = [new MfmCodeBlockNode("asd\nsdf", "cs")];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestHashtag()
	{
		const string input = "test #test #test's #t-e_s-t. test";

		List<MfmNode> expected =
		[
			new MfmTextNode("test "),
			new MfmHashtagNode("test"),
			new MfmTextNode(" "),
			new MfmHashtagNode("test"),
			new MfmTextNode("'s "),
			new MfmHashtagNode("t-e_s-t"),
			new MfmTextNode(". test")
		];
		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestUrl()
	{
		const string  input    = "https://example.org/path/Name_(test)_asdf";
		List<MfmNode> expected = [new MfmUrlNode(input, false)];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestUrlAlt()
	{
		const string  input    = "https://example.org/path/Name_(test";
		List<MfmNode> expected = [new MfmUrlNode(input, false)];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestUrlNeg()
	{
		const string  input    = "https://example.org/path/Name_test)_asdf";
		List<MfmNode> expected = [new MfmUrlNode(input[..34], false), new MfmTextNode(input[34..])];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestLink()
	{
		const string  input    = "[test](https://example.org/path/Name_(test)_asdf)";
		List<MfmNode> expected = [new MfmLinkNode("https://example.org/path/Name_(test)_asdf", "test", false)];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}
	
	[TestMethod]
	public void TestLinkSilent()
	{
		const string  input    = "?[test](https://example.org/path/Name_(test)_asdf)";
		List<MfmNode> expected = [new MfmLinkNode("https://example.org/path/Name_(test)_asdf", "test", true)];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestLinkNeg()
	{
		const string input = "[test](https://example.org/path/Name_(test_asdf)";
		List<MfmNode> expected =
		[
			new MfmTextNode("[test]("), new MfmUrlNode("https://example.org/path/Name_(test_asdf)", false)
		];
		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestQuote()
	{
		const string  input1   = "> this is a quote";
		const string  input2   = ">this is a quote";
		List<MfmNode> expected = [new MfmQuoteNode([new MfmTextNode("this is a quote")], false, true)];

		var res1 = Mfm.parse(input1);
		var res2 = Mfm.parse(input2);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res1.ToList().Should().Equal(expected, MfmNodeEqual);
		res2.ToList().Should().Equal(expected, MfmNodeEqual);

		MfmSerializer.Serialize(res1).Should().BeEquivalentTo(input1);
		MfmSerializer.Serialize(res2).Should().BeEquivalentTo(input1);
	}

	[TestMethod]
	public void TestQuoteInline()
	{
		const string input =
			"""
			this is plain text > this is not a quote >this is also not a quote
			> this is a quote
			> this is part of the same quote
			>this too

			this is some plain text inbetween
			>this is a second quote
			> this is part of the second quote

			> this is a third quote
			and this is some plain text to close it off
			""";

		const string canonical =
			"""
			this is plain text > this is not a quote >this is also not a quote
			> this is a quote
			> this is part of the same quote
			> this too
			this is some plain text inbetween
			> this is a second quote
			> this is part of the second quote

			> this is a third quote
			and this is some plain text to close it off
			""";

		// @formatter:off
		List<MfmNode> expected =
		[
			new MfmTextNode("this is plain text > this is not a quote >this is also not a quote\n"),
			new MfmQuoteNode([new MfmTextNode("this is a quote\nthis is part of the same quote\nthis too")], false, false),
			new MfmTextNode("this is some plain text inbetween\n"),
			new MfmQuoteNode([new MfmTextNode("this is a second quote\nthis is part of the second quote")], true, false),
			new MfmQuoteNode([new MfmTextNode("this is a third quote")], false, false),
			new MfmTextNode("and this is some plain text to close it off")
		];
		// @formatter:on

		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);

		MfmSerializer.Serialize(res).Should().BeEquivalentTo(canonical);
	}

	[TestMethod]
	public void TestFn()
	{
		const string input =
			"test $[] $[test] $[test ] $[test test] $[test123 test] $[test.a test] $[test.a=b test] $[test.a=b,c=e test] $[test.a,c=e test] $[test.a=b,c test]";

		List<MfmNode> expected =
		[
			new MfmTextNode("test $[] $[test] $[test ] "),
			new MfmFnNode("test", null, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test123", null, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test", new FSDict { { "a", null } }, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test", new FSDict { { "a", "b" } }, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test", new FSDict { { "a", "b" }, { "c", "e" } }, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test", new FSDict { { "a", null }, { "c", "e" } }, [new MfmTextNode("test")]),
			new MfmTextNode(" "),
			new MfmFnNode("test", new FSDict { { "a", "b" }, { "c", null } }, [new MfmTextNode("test")]),
		];

		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);

		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestComplexFnRoundtrip()
	{
		// MFM art by https://github.com/ChaoticLeah
		const string input = """
		                     <center>
		                     $[scale.x=10,y=10 $[scale.x=10,y=110 $[scale.x=10,y=10 ⬛]]]
		                     $[position.y=9 :neocat:                                                  :neocat_aww:]
		                     $[position.y=4.3 $[border.radius=20                                                                        ]
		                     ]
		                     $[followmouse.x $[position.y=.8 $[scale.x=5,y=5  $[scale.x=0.5,y=0.5 
		                     ⚪$[position.x=-16.5 $[scale.x=10 $[scale.x=10 ⬛]]]$[position.x=16.5,y=-1.3 $[scale.x=10 $[scale.x=10 ⬛]]]]]]]


		                     $[position.x=-88 $[scale.x=10,y=10 $[scale.x=10,y=110 $[scale.x=10,y=10 ⬛]]]]
		                     $[position.x=88 $[scale.x=10,y=10 $[scale.x=10,y=110 $[scale.x=10,y=10 ⬛]]]]

		                     $[position.y=-10 Neocat Awwww Slider]
		                     </center>
		                     """;

		var res = Mfm.parse(input);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	private static bool MfmNodeEqual(MfmNode a, MfmNode b)
	{
		if (a.GetType() != b.GetType()) return false;

		if (!a.Children.IsEmpty || !b.Children.IsEmpty)
		{
			if ((!a.Children.IsEmpty && b.Children.IsEmpty) || (a.Children.IsEmpty && !b.Children.IsEmpty))
				return false;
			if (!a.Children.SequenceEqual(b.Children, new MfmNodeEquality()))
				return false;
		}

		switch (a)
		{
			case MfmTextNode textNode when ((MfmTextNode)b).Text != textNode.Text:
				return false;
			case MfmItalicNode ax:
			{
				var bx = (MfmItalicNode)b;
				if (!bx.Type.Equals(ax.Type)) return false;
				break;
			}
			case MfmBoldNode ax:
			{
				var bx = (MfmBoldNode)b;
				if (!bx.Type.Equals(ax.Type)) return false;
				break;
			}
			case MfmStrikeNode ax:
			{
				var bx = (MfmStrikeNode)b;
				if (!bx.Type.Equals(ax.Type)) return false;
				break;
			}
			case MfmMentionNode ax:
			{
				var bx = (MfmMentionNode)b;
				if (bx.Acct != ax.Acct) return false;
				if (bx.Username != ax.Username) return false;
				if (bx.Host?.Value != ax.Host?.Value) return false;
				break;
			}
			case MfmCodeBlockNode ax:
			{
				var bx = (MfmCodeBlockNode)b;
				if (ax.Code != bx.Code) return false;
				if (ax.Language?.Value != bx.Language?.Value) return false;
				break;
			}
			case MfmInlineCodeNode ax:
			{
				var bx = (MfmInlineCodeNode)b;
				if (ax.Code != bx.Code) return false;
				break;
			}
			case MfmMathBlockNode ax:
			{
				var bx = (MfmMathBlockNode)b;
				if (ax.Formula != bx.Formula) return false;
				if (ax.Formula != bx.Formula) return false;
				break;
			}
			case MfmMathInlineNode ax:
			{
				var bx = (MfmMathInlineNode)b;
				if (ax.Formula != bx.Formula) return false;
				if (ax.Formula != bx.Formula) return false;
				break;
			}
			case MfmSearchNode searchNode:
			{
				var bx = (MfmSearchNode)b;
				if (searchNode.Query != bx.Query) return false;
				if (searchNode.Content != bx.Content) return false;
				break;
			}
			case MfmEmojiCodeNode ax:
			{
				var bx = (MfmEmojiCodeNode)b;
				if (ax.Name != bx.Name) return false;
				break;
			}
			case MfmHashtagNode ax:
			{
				var bx = (MfmHashtagNode)b;
				if (ax.Hashtag != bx.Hashtag) return false;
				break;
			}
			case MfmUrlNode ax:
			{
				var bx = (MfmUrlNode)b;
				if (ax.Url != bx.Url) return false;
				if (ax.Brackets != bx.Brackets) return false;
				break;
			}
			case MfmLinkNode ax:
			{
				var bx = (MfmLinkNode)b;
				if (ax.Url != bx.Url) return false;
				if (ax.Silent != bx.Silent) return false;
				break;
			}
			case MfmQuoteNode ax:
			{
				var bx = (MfmQuoteNode)b;
				if (ax.FollowedByEof != bx.FollowedByEof) return false;
				if (ax.FollowedByQuote != bx.FollowedByQuote) return false;
				break;
			}
			case MfmFnNode ax:
			{
				var bx = (MfmFnNode)b;
				if (ax.Name != bx.Name) return false;
				if ((ax.Args == null) != (bx.Args == null)) return false;
				if (ax.Args == null || bx.Args == null) return true;
				if (ax.Args.Value.Count != bx.Args.Value.Count) return false;
				// ReSharper disable once UsageOfDefaultStructEquality
				if (ax.Args.Value.Except(bx.Args.Value).Any()) return false;

				break;
			}
		}

		return true;
	}

	private class MfmNodeEquality : IEqualityComparer<MfmNode>
	{
		public bool Equals(MfmNode? x, MfmNode? y)
		{
			if (x == null && y == null) return true;
			if (x == null && y != null) return false;
			if (x != null && y == null) return false;

			return MfmNodeEqual(x!, y!);
		}

		public int GetHashCode(MfmNode obj)
		{
			return obj.GetHashCode();
		}
	}
}