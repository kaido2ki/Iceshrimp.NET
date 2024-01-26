using System.Globalization;

namespace Iceshrimp.Backend.Core.Extensions;

public static class StringExtensions {
	public static string Truncate(this string target, int maxLength) {
		return target[..Math.Min(target.Length, maxLength)];
	}

	public static string ToPunycode(this string target) {
		return new IdnMapping().GetAscii(target);
	}

	public static string FromPunycode(this string target) {
		return new IdnMapping().GetUnicode(target);
	}
}