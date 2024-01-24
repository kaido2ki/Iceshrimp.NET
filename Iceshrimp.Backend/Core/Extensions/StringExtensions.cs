namespace Iceshrimp.Backend.Core.Extensions;

public static class StringExtensions {
	public static string Truncate(this string target, int maxLength) {
		return target[..Math.Min(target.Length, maxLength)];
	}
}