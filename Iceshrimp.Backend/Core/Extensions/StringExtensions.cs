using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using EntityFrameworkCore.Projectables;

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

[SuppressMessage("ReSharper", "StringCompareToIsCultureSpecific")]
public static class ProjectableStringExtensions {
	[Projectable]
	public static bool IsLessThan(this string a, string b) => a.CompareTo(b) < 0;

	[Projectable]
	public static bool IsLessOrEqualTo(this string a, string b) => a.CompareTo(b) <= 0;

	[Projectable]
	public static bool IsGreaterThan(this string a, string b) => a.CompareTo(b) > 0;

	[Projectable]
	public static bool IsGreaterOrEqualTo(this string a, string b) => a.CompareTo(b) >= 0;
}