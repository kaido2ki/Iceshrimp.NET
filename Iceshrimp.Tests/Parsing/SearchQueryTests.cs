using Iceshrimp.Parsing;
using static Iceshrimp.Parsing.SearchQueryFilters;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class SearchQueryTests
{
	private static List<Filter> GetCandidatesByUsername(IEnumerable<string> candidates) =>
		candidates.Select(p => $"{p}:username").SelectMany(SearchQuery.parse).ToList();

	private static void Validate(ICollection<Filter> results, object expectedResult, int count)
	{
		results.Count.Should().Be(count);
		foreach (var res in results) res.Should().BeEquivalentTo(expectedResult);
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseFrom(bool negated)
	{
		List<string> candidates = ["from", "author", "by", "user"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new FromFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseMention(bool negated)
	{
		List<string> candidates = ["mention", "mentions", "mentioning"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new MentionFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseReply(bool negated)
	{
		List<string> candidates = ["reply", "replying", "to"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new ReplyFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseInstance(bool negated)
	{
		List<string> candidates = ["instance", "domain", "host"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = candidates.Select(p => $"{p}:instance.tld").SelectMany(SearchQuery.parse).ToList();
		var expectedResult      = new InstanceFilter(negated, "instance.tld");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	public void TestParseAfter()
	{
		List<string> candidates     = ["after", "since"];
		var          results        = candidates.Select(p => $"{p}:2024-03-01").SelectMany(SearchQuery.parse).ToList();
		var          expectedResult = new AfterFilter(DateOnly.ParseExact("2024-03-01", "O"));
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	public void TestParseBefore()
	{
		List<string> candidates     = ["before", "until"];
		var          results        = candidates.Select(p => $"{p}:2024-03-01").SelectMany(SearchQuery.parse).ToList();
		var          expectedResult = new BeforeFilter(DateOnly.ParseExact("2024-03-01", "O"));
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseAttachment(bool negated)
	{
		List<string> keyCandidates = ["has", "attachment", "attached"];
		if (negated) keyCandidates = keyCandidates.Select(p => "-" + p).ToList();
		List<string> candidates    = ["any", "image", "video", "audio", "file"];
		var results =
			keyCandidates.Select(k => candidates.Select(v => $"{k}:{v}").SelectMany(SearchQuery.parse).ToList());
		List<Filter> expectedResults =
		[
			new AttachmentFilter(negated, "any"),
			new AttachmentFilter(negated, "image"),
			new AttachmentFilter(negated, "video"),
			new AttachmentFilter(negated, "audio"),
			new AttachmentFilter(negated, "file")
		];
		results.Should()
		       .HaveCount(keyCandidates.Count)
		       .And.AllBeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	public void TestParseCase()
	{
		const string key             = "case";
		List<string> candidates      = ["sensitive", "insensitive"];
		var          results         = candidates.Select(v => $"{key}:{v}").SelectMany(SearchQuery.parse).ToList();
		List<Filter> expectedResults = [new CaseFilter("sensitive"), new CaseFilter("insensitive")];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	public void TestParseMatch()
	{
		const string key        = "match";
		List<string> candidates = ["words", "word", "substr", "substring"];
		var          results    = candidates.Select(v => $"{key}:{v}").SelectMany(SearchQuery.parse).ToList();
		List<Filter> expectedResults =
		[
			new MatchFilter("words"), new MatchFilter("words"), new MatchFilter("substr"), new MatchFilter("substr")
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseIn(bool negated)
	{
		var          key        = negated ? "-in" : "in";
		List<string> candidates = ["bookmarks", "likes", "favorites", "favourites", "reactions"];
		var          results    = candidates.Select(v => $"{key}:{v}").SelectMany(SearchQuery.parse).ToList();
		List<Filter> expectedResults =
		[
			new InFilter(negated, "bookmarks"),
			new InFilter(negated, "likes"),
			new InFilter(negated, "likes"),
			new InFilter(negated, "likes"),
			new InFilter(negated, "reactions")
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseMisc(bool negated)
	{
		var key = negated ? "-filter" : "filter";
		List<string> candidates =
		[
			"followers", "following", "replies", "reply", "renote", "renotes", "boosts", "boost"
		];
		var results = candidates.Select(v => $"{key}:{v}").SelectMany(SearchQuery.parse).ToList();
		List<Filter> expectedResults =
		[
			new MiscFilter(negated, "followers"),
			new MiscFilter(negated, "following"),
			new MiscFilter(negated, "replies"),
			new MiscFilter(negated, "replies"),
			new MiscFilter(negated, "renotes"),
			new MiscFilter(negated, "renotes"),
			new MiscFilter(negated, "renotes"),
			new MiscFilter(negated, "renotes"),
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false), DataRow(true)]
	public void TestParseWord(bool negated)
	{
		List<string> candidates = ["test", "word", "since:2023-10-10invalid", "in:bookmarkstypo"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = candidates.Select(v => $"{v}").SelectMany(SearchQuery.parse).ToList();
		List<Filter> expectedResults =
		[
			new WordFilter(negated, "test"),
			new WordFilter(negated, "word"),
			new WordFilter(negated, "since:2023-10-10invalid"),
			new WordFilter(negated, "in:bookmarkstypo")
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	public void TestParseMultiWord()
	{
		const string input   = "(word OR word2 OR word3)";
		var          results = SearchQuery.parse(input).ToList();
		results.Should().HaveCount(1);
		results[0].Should().BeOfType<MultiWordFilter>();
		((MultiWordFilter)results[0]).Values.ToList().Should().BeEquivalentTo(["word", "word2", "word3"]);
	}

	[TestMethod]
	public void TestParseLiteralString()
	{
		const string input   = "\"literal string with spaces $# and has:image before:2023-10-10 other things\"";
		var          results = SearchQuery.parse(input).ToList();
		results.Should().HaveCount(1);
		results[0].Should().BeOfType<WordFilter>();
		((WordFilter)results[0]).Value.Should()
		                        .BeEquivalentTo("literal string with spaces $# and has:image before:2023-10-10 other things");
	}
}