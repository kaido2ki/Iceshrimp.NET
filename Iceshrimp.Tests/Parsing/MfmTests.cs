using Iceshrimp.Backend.Core.Helpers.LibMfm.Serialization;
using Iceshrimp.Parsing;
using Microsoft.FSharp.Collections;
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

		var res = Mfm.parse("*italic **bold** italic*").ToList();
		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.Should().Equal(expected, MfmNodeEqual);
	}

	[TestMethod]
	public void TestParseCode()
	{
		List<MfmNode> expected =
		[
			new MfmInlineCodeNode("test"),
			new MfmTextNode("\n"),
			new MfmCodeBlockNode("test", null),
			new MfmTextNode("\n"),
			new MfmCodeBlockNode("test", "lang")
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
		const string input = "test @test test";
		List<MfmNode> expected =
		[
			new MfmTextNode("test "), new MfmMentionNode("test", "test", null), new MfmTextNode(" test")
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
	public void TestCodeBlock()
	{
		const string input = """
		                     test 123
		                     ```
		                     this is a code block
		                     ```
		                     test 123
		                     """;
		List<MfmNode> expected =
		[
			new MfmTextNode("test 123\n"),
			new MfmCodeBlockNode("this is a code block", null),
			new MfmTextNode("\ntest 123")
		];
		var res = Mfm.parse(input);

		AssertionOptions.FormattingOptions.MaxDepth = 100;
		res.ToList().Should().Equal(expected, MfmNodeEqual);
		MfmSerializer.Serialize(res).Should().BeEquivalentTo(input);
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
		const string input = "test #test #test's #test. test";

		List<MfmNode> expected =
		[
			new MfmTextNode("test "),
			new MfmHashtagNode("test"),
			new MfmTextNode(" "),
			new MfmHashtagNode("test"),
			new MfmTextNode("'s "),
			new MfmHashtagNode("test"),
			new MfmTextNode(". test"),
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
			var pre = DateTime.Now;
			Mfm.parse(mfm);
			var post = DateTime.Now;
			var ms   = (post - pre).TotalMilliseconds;
			Console.WriteLine($"Took {ms} ms");
			return ms;
		}
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

	private static bool MfmNodeEqual(MfmNode a, MfmNode b)
	{
		if (a.GetType() != b.GetType()) return false;

		if (!a.Children.IsEmpty || !b.Children.IsEmpty)
		{
			if (!a.Children.IsEmpty && b.Children.IsEmpty || a.Children.IsEmpty && !b.Children.IsEmpty)
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
			case MfmFnNode ax:
			{
				var bx = (MfmFnNode)b;
				if (ax.Args != bx.Args) return false;
				if (ax.Name != bx.Name) return false;
				break;
			}
		}

		return true;
	}
}