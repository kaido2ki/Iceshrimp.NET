using Iceshrimp.Parsing;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class SearchQueryTests
{
	private static List<ISearchQueryFilter> GetCandidatesByUsername(IEnumerable<string> candidates) =>
		candidates.Select(p => $"{p}:username").SelectMany(p => SearchQueryParser.Parse(p)).ToList();

	private static void Validate(ICollection<ISearchQueryFilter> results, object expectedResult, int count)
	{
		results.Count.Should().Be(count);
		foreach (var res in results) res.Should().BeEquivalentTo(expectedResult);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseCw(bool negated)
	{
		var result         = SearchQueryParser.Parse(negated ? "-cw:meta" : "cw:meta").ToList();
		var expectedResult = new CwFilter(negated, "meta");
		Validate(result, expectedResult, 1);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseFrom(bool negated)
	{
		List<string> candidates = ["from", "author", "by", "user"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new FromFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseInvalid(bool negated)
	{
		var prefix = negated ? "-" : "";
		//SearchQueryParser.Parse($"{prefix}from:");
		//SearchQueryParser.Parse($"{prefix}:");
		SearchQueryParser.Parse($"{prefix}asd {prefix}:");
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseMention(bool negated)
	{
		List<string> candidates = ["mention", "mentions", "mentioning"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new MentionFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseReply(bool negated)
	{
		List<string> candidates = ["reply", "replying", "to"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = GetCandidatesByUsername(candidates);
		var expectedResult      = new ReplyFilter(negated, "username");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseInstance(bool negated)
	{
		List<string> candidates = ["instance", "domain", "host"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results = candidates.Select(p => $"{p}:instance.tld").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		var expectedResult = new InstanceFilter(negated, "instance.tld");
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	public void TestParseAfter()
	{
		List<string> candidates = ["after", "since"];
		var results = candidates.Select(p => $"{p}:2024-03-01").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		var expectedResult = new AfterFilter(DateOnly.ParseExact("2024-03-01", "O"));
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	public void TestParseBefore()
	{
		List<string> candidates = ["before", "until"];
		var results = candidates.Select(p => $"{p}:2024-03-01").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		var expectedResult = new BeforeFilter(DateOnly.ParseExact("2024-03-01", "O"));
		Validate(results, expectedResult, candidates.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseAttachment(bool negated)
	{
		List<string> keyCandidates = ["has", "attachment", "attached"];
		if (negated) keyCandidates = keyCandidates.Select(p => "-" + p).ToList();
		List<string> candidates    = ["any", "media", "image", "video", "audio", "file", "poll"];
		var results =
			keyCandidates.Select(k => candidates.Select(v => $"{k}:{v}")
			                                    .SelectMany(p => SearchQueryParser.Parse(p))
			                                    .ToList());
		List<ISearchQueryFilter> expectedResults =
		[
			new AttachmentFilter(negated, AttachmentFilterType.Media),
			new AttachmentFilter(negated, AttachmentFilterType.Media),
			new AttachmentFilter(negated, AttachmentFilterType.Image),
			new AttachmentFilter(negated, AttachmentFilterType.Video),
			new AttachmentFilter(negated, AttachmentFilterType.Audio),
			new AttachmentFilter(negated, AttachmentFilterType.File),
			new AttachmentFilter(negated, AttachmentFilterType.Poll)
		];
		results.Should()
		       .HaveCount(keyCandidates.Count)
		       .And.AllBeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	public void TestParseCase()
	{
		const string key = "case";
		List<string> candidates = ["sensitive", "insensitive"];
		var results = candidates.Select(v => $"{key}:{v}").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		List<ISearchQueryFilter> expectedResults =
		[
			new CaseFilter(CaseFilterType.Sensitive), new CaseFilter(CaseFilterType.Insensitive)
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	public void TestParseMatch()
	{
		const string key = "match";
		List<string> candidates = ["words", "word", "substr", "substring"];
		var results = candidates.Select(v => $"{key}:{v}").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		List<ISearchQueryFilter> expectedResults =
		[
			new MatchFilter(MatchFilterType.Words),
			new MatchFilter(MatchFilterType.Words),
			new MatchFilter(MatchFilterType.Substring),
			new MatchFilter(MatchFilterType.Substring)
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseIn(bool negated)
	{
		var key = negated ? "-in" : "in";
		List<string> candidates = ["bookmarks", "likes", "favorites", "favourites", "reactions", "interactions"];
		var results = candidates.Select(v => $"{key}:{v}").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		List<ISearchQueryFilter> expectedResults =
		[
			new InFilter(negated, InFilterType.Bookmarks),
			new InFilter(negated, InFilterType.Likes),
			new InFilter(negated, InFilterType.Likes),
			new InFilter(negated, InFilterType.Likes),
			new InFilter(negated, InFilterType.Reactions),
			new InFilter(negated, InFilterType.Interactions)
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseMisc(bool negated)
	{
		var key = negated ? "-filter" : "filter";
		List<string> candidates =
		[
			"followers", "following", "replies", "reply", "renote", "renotes", "boosts", "boost"
		];
		var results = candidates.Select(v => $"{key}:{v}").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		List<ISearchQueryFilter> expectedResults =
		[
			new MiscFilter(negated, MiscFilterType.Followers),
			new MiscFilter(negated, MiscFilterType.Following),
			new MiscFilter(negated, MiscFilterType.Replies),
			new MiscFilter(negated, MiscFilterType.Replies),
			new MiscFilter(negated, MiscFilterType.Renotes),
			new MiscFilter(negated, MiscFilterType.Renotes),
			new MiscFilter(negated, MiscFilterType.Renotes),
			new MiscFilter(negated, MiscFilterType.Renotes)
		];
		results.Should()
		       .HaveCount(expectedResults.Count)
		       .And.BeEquivalentTo(expectedResults, opts => opts.RespectingRuntimeTypes());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void TestParseWord(bool negated)
	{
		List<string> candidates = ["test", "word", "since:2023-10-10invalid", "in:bookmarkstypo"];
		if (negated) candidates = candidates.Select(p => "-" + p).ToList();
		var results             = candidates.Select(v => $"{v}").SelectMany(p => SearchQueryParser.Parse(p)).ToList();
		List<ISearchQueryFilter> expectedResults =
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
		var          results = SearchQueryParser.Parse(input).ToList();
		results.Should().HaveCount(1);
		results[0].Should().BeOfType<MultiWordFilter>();
		((MultiWordFilter)results[0]).Values.ToList().Should().BeEquivalentTo(["word", "word2", "word3"]);
	}

	[TestMethod]
	public void TestParseLiteralString()
	{
		const string input   = "\"literal string with spaces $# and has:image before:2023-10-10 other things\"";
		var          results = SearchQueryParser.Parse(input).ToList();
		results.Should().HaveCount(1);
		results[0].Should().BeOfType<WordFilter>();
		((WordFilter)results[0]).Value.Should()
		                        .BeEquivalentTo("literal string with spaces $# and has:image before:2023-10-10 other things");
	}
}
