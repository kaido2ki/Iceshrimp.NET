using System.Diagnostics;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Iceshrimp.Parsing;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using static Iceshrimp.Parsing.MfmNodeTypes;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class MfmTests
{
	[TestMethod]
	public void TestParseBoldItalic()
	{
		List<MfmNode> expected =
		[
			new MfmItalicNode(ListModule.OfSeq<MfmInlineNode>([
				new MfmTextNode("italic "),
				new MfmBoldNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("bold")])),
				new MfmTextNode(" italic")
			]))
		];

		var res         = Mfm.parse("*italic **bold** italic*").ToList();
		var resAlt      = Mfm.parse("_italic **bold** italic_").ToList();
		var resAlt2     = Mfm.parse("_italic __bold__ italic_").ToList();
		var resAlt3     = Mfm.parse("<i>italic <b>bold</b> italic</i>").ToList();
		var resMixed    = Mfm.parse("<i>italic **bold** italic</i>").ToList();
		var resMixedAlt = Mfm.parse("*italic <b>bold</b> italic*").ToList();

		AssertionOptions.FormattingOptions.MaxDepth = 100;

		res.Should().Equal(expected, MfmNodeEqual);
		resAlt.Should().Equal(expected, MfmNodeEqual);
		resAlt2.Should().Equal(expected, MfmNodeEqual);
		resAlt3.Should().Equal(expected, MfmNodeEqual);
		resMixed.Should().Equal(expected, MfmNodeEqual);
		resMixedAlt.Should().Equal(expected, MfmNodeEqual);
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
			new MfmItalicNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("test")])),
			new MfmTextNode("test")
		];

		Mfm.parse("test *test*test").ToList().Should().Equal(expected, MfmNodeEqual);
		Mfm.parse("test _test_test").ToList().Should().Equal(expected, MfmNodeEqual);
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
		const string input = "https://example.org/path/Name_(test)_asdf";
		//TODO: List<MfmNode> expected = [new MfmUrlNode(input, false),];
		List<MfmNode> expected = [new MfmUrlNode(input[..30], false), new MfmTextNode(input[30..])];
		var           res      = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
	}

	[TestMethod]
	public void TestQuote()
	{
		const string input1 = "> this is a quote";
		const string input2 = ">this is a quote";
		List<MfmNode> expected =
		[
			new MfmQuoteNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("this is a quote")]), false, true)
		];

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
			new MfmQuoteNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("this is a quote\nthis is part of the same quote\nthis too")]), false, false),
			new MfmTextNode("this is some plain text inbetween\n"),
			new MfmQuoteNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("this is a second quote\nthis is part of the second quote")]), true, false),
			new MfmQuoteNode(ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("this is a third quote")]), false, false),
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
			"test $[] $[test] $[test ] $[test test] $[test.a test] $[test.a=b test] $[test.a=b,c=e test] $[test.a,c=e test] $[test.a=b,c test]";

		var some = FSharpOption<IDictionary<string, FSharpOption<string>>>.Some;
		var none = FSharpOption<IDictionary<string, FSharpOption<string>>>.None;
		var test = ListModule.OfSeq<MfmInlineNode>([new MfmTextNode("test")]);
		
		// @formatter:off
		List<MfmNode> expected =
		[
			new MfmTextNode("test $[] $[test] $[test ] "),
			new MfmFnNode("test",
			              none,
			              test),
			new MfmTextNode(" "),
			new MfmFnNode("test",
			              some(new Dictionary<string, FSharpOption<string>>{ {"a", FSharpOption<string>.None} }),
			              test),
			new MfmTextNode(" "),
			new MfmFnNode("test",
			              some(new Dictionary<string, FSharpOption<string>>{ {"a", FSharpOption<string>.Some("b")} }),
			              test),
			new MfmTextNode(" "),
			new MfmFnNode("test",
			              some(new Dictionary<string, FSharpOption<string>>{ {"a", FSharpOption<string>.Some("b")}, {"c", FSharpOption<string>.Some("e")} }),
			              test),
			new MfmTextNode(" "),
			new MfmFnNode("test",
			              some(new Dictionary<string, FSharpOption<string>>{ {"a", FSharpOption<string>.None}, {"c", FSharpOption<string>.Some("e")} }),
			              test),
			new MfmTextNode(" "),
			new MfmFnNode("test",
			              some(new Dictionary<string, FSharpOption<string>>{ {"a", FSharpOption<string>.Some("b")}, {"c", FSharpOption<string>.None} }),
			              test),
		];
		// @formatter:on

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

	[TestMethod]
	public void Benchmark()
	{
		const string mfm =
			"<plain>*blabla*</plain> *test* #example @example @example@invalid @example@example.com @invalid:matrix.org https://hello.com http://test.de <https://大石泉すき.example.com> javascript://sdfgsdf [test](https://asdfg) ?[test](https://asdfg) `asd`";

		double duration                      = 100;
		for (var i = 0; i < 4; i++) duration = RunBenchmark();

		duration.Should().BeLessThan(2);

		return;

		double RunBenchmark()
		{
			var pre = Stopwatch.GetTimestamp();
			Mfm.parse(mfm);
			var ms = Stopwatch.GetElapsedTime(pre).GetTotalMilliseconds();
			Console.WriteLine($@"Took {ms} ms");
			return ms;
		}
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