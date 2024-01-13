namespace Iceshrimp.Backend.Core.Helpers;

public static class StringExtensions {
	public static string Truncate(this string target, int maxLength) => target[..Math.Min(target.Length, maxLength)];
}