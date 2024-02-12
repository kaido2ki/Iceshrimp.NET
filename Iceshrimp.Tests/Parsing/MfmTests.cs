using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;
using Microsoft.Extensions.DependencyInjection;

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
			var provider  = MockObjects.ServiceProvider;
			var converter = provider.GetRequiredService<MfmConverter>();

			var pre = DateTime.Now;
			await converter.ToHtmlAsync(Mfm, [], null);
			var post = DateTime.Now;
			var ms   = (post - pre).TotalMilliseconds;
			Console.WriteLine($"Took {ms} ms");
			return ms;
		}
	}

	//TODO: TestFromHtml
	//TODO: RoundtripTest
}