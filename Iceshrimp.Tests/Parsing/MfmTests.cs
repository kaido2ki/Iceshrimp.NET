using Iceshrimp.MfmSharp.Conversion;
using Iceshrimp.MfmSharp.Parsing;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class MfmTests {
	private const string Mfm =
		"<plain>*blabla*</plain> *test* #example @example @example@invalid @example@example.com @invalid:matrix.org https://hello.com http://test.de <https://大石泉すき.example.com> javascript://sdfgsdf [test](https://asdfg) ?[test](https://asdfg) `asd`";

	[TestMethod]
	public void TestParse() {
		//TODO: actually validate the output (this currently only checks that no exception is thrown)
		MfmParser.Parse(Mfm);
	}

	[TestMethod]
	public async Task TestToHtml() {
		double duration                      = 100;
		for (var i = 0; i < 4; i++) duration = await Benchmark();

		duration.Should().BeLessThan(2);

		return;

		async Task<double> Benchmark() {
			var pre = DateTime.Now;
			await MfmConverter.ToHtmlAsync(Mfm);
			var post = DateTime.Now;
			var ms   = (post - pre).TotalMilliseconds;
			Console.WriteLine($"Took {ms} ms");
			return ms;
		}
	}

	//TODO: TestFromHtml
	//TODO: RoundtripTest
}