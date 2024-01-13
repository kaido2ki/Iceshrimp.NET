using System.Text;

namespace Iceshrimp.Backend.Core.Helpers;

public static class NumberExtensions {
	private const string Base36Charset = "0123456789abcdefghijklmnopqrstuvwxyz";

	public static string ToBase36(this long input) {
		if (input == 0) return "0";
		var result = new StringBuilder();

		while (input > 0) {
			result.Insert(0, Base36Charset[(int)(input % 36)]);
			input /= 36;
		}

		return result.ToString();
	}

	public static string ToBase36(this int input) {
		if (input == 0) return "0";
		var result = new StringBuilder();

		while (input >= 0) {
			result.Insert(0, Base36Charset[input % 36]);
			input /= 36;
		}

		return result.ToString();
	}
}