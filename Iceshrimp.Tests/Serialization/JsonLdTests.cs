using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;

namespace Iceshrimp.Tests.Serialization;

[TestClass]
public class JsonLdTests {
	private ASActor _actor = null!;

	[TestInitialize]
	public void Initialize() {
		_actor = MockObjects.ASActor;
	}

	[TestMethod]
	public void RoundtripTest() {
		var expanded = LdHelpers.Expand(_actor)!;
		expanded.Should().NotBeNull();

		var canonicalized = LdHelpers.Canonicalize(expanded);
		canonicalized.Should().NotBeNull();

		var compacted = LdHelpers.Compact(expanded);
		compacted.Should().NotBeNull();

		var expanded2 = LdHelpers.Expand(compacted)!;
		expanded2.Should().NotBeNull();
		expanded2.Should().BeEquivalentTo(expanded);

		var compacted2 = LdHelpers.Compact(expanded2)!;
		compacted2.Should().NotBeNull();
		compacted2.Should().BeEquivalentTo(compacted);

		var canonicalized2 = LdHelpers.Canonicalize(expanded2);
		canonicalized2.Should().NotBeNull();
		canonicalized2.Should().BeEquivalentTo(canonicalized);
	}
}