using Iceshrimp.Parsing;
using static Iceshrimp.Parsing.MfmNodeTypes;

namespace Iceshrimp.Backend.Core.Helpers.LibMfm.Parsing;

public static class MfmParser
{
	public static IEnumerable<MfmNode> Parse(string input) => Mfm.parse(input);
}