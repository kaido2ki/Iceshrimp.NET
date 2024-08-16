using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Tests.Parsing;

[TestClass]
public class EmojiTests
{
	private static void TestEmojiRegexTemplate(string input, bool expectedOutput) =>
		EmojiHelpers.IsEmoji(input).Should().Be(expectedOutput);

	[TestMethod]
	[DataRow("\ud83d\ude84")] // high speed train (E1.0/U6.0)
	[DataRow("\ud83c\udfc2\ud83c\udffd")] // snowboarder: medium skin tone (E2.0)
	[DataRow("\ud83e\udd23")] // rolling on the floor laughing (E3.0/U9.0)
	[DataRow("\ud83c\uddfa\ud83c\uddf3")] // flag: united nations (E4.0)
	[DataRow("\ud83e\udddc\ud83c\udffc")] // merperson: medium-light skin tone (E5.0/U10.0)
	[DataRow("\ud83d\udc69\ud83c\udffe\u200d\ud83e\uddb1")] // woman: medium-dark skin tone, curly hair (E11.0)
	[DataRow("\ud83d\udc68\ud83c\udffe\u200d\ud83e\udd1d\u200d\ud83d\udc68\ud83c\udffd")] // men holding hands: medium-dark skin tone, medium skin tone (E12.0)
	[DataRow("\ud83e\uddd1\ud83c\udffc\u200d\ud83d\ude80")] // astronaut: medium-light skin tone (E12.1)
	[DataRow("\ud83e\udd72")] // smiling face with tear (E13.0)
	[DataRow("\ud83d\ude35\u200d\ud83d\udcab")] // face with spiral eyes (E13.1)
	[DataRow("\ud83e\udee0")] // melting face (E14.0)
	[DataRow("\ud83e\udebc")] // jellyfish (E15.0)
	[DataRow("\ud83e\ude75")] // light blue heart (E15.0)
	[DataRow("\ud83d\ude42\u200d\u2194\ufe0f")] // head shaking horizontally (E15.1)
	public void TestEmojiRegexEmoji(string input) => TestEmojiRegexTemplate(input, true);

	[TestMethod]
	[DataRow("test")]
	[DataRow("1")]
	public void TestEmojiRegexPlainText(string input) => TestEmojiRegexTemplate(input, false);

	[TestMethod]
	[DataRow("\u2122", "\ufe0f")]       // trademark sign
	[DataRow("\ud83d\udd74", "\ufe0f")] // man in business suit levitating
	[DataRow("\u2764", "\ufe0f")]       // heavy black heart / red heart
	public void TestEmojiRegexEmojiSelector(string input, string selector)
	{
		TestEmojiRegexTemplate(input, false);
		TestEmojiRegexTemplate(input + selector, true);
	}
}