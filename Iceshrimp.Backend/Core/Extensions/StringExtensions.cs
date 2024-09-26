using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using EntityFrameworkCore.Projectables;

namespace Iceshrimp.Backend.Core.Extensions;

public static class StringExtensions
{
	private static readonly IdnMapping IdnMapping = new();

	public static bool EqualsInvariant(this string? s1, string? s2) =>
		string.Equals(s1, s2, StringComparison.InvariantCulture);

	public static bool EqualsIgnoreCase(this string? s1, string s2) =>
		string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);

	public static string Truncate(this string target, int maxLength)
	{
		return target[..Math.Min(target.Length, maxLength)];
	}
	
	public static string TruncateEllipsis(this string target, int maxLength)
	{
		if (target.Length <= maxLength) return target;
		return target[..(maxLength-3)] + "...";
	}

	private static string ToPunycode(this string target)
	{
		return IdnMapping.GetAscii(target);
	}

	public static string ToPunycodeLower(this string target)
	{
		return ToPunycode(target).ToLowerInvariant();
	}

	public static string FromPunycode(this string target)
	{
		return IdnMapping.GetUnicode(target);
	}

	public static string ToTitleCase(this string input) => input switch
	{
		null => throw new ArgumentNullException(nameof(input)),
		""   => throw new ArgumentException(@$"{nameof(input)} cannot be empty", nameof(input)),
		_    => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
	};
}

[SuppressMessage("ReSharper", "StringCompareToIsCultureSpecific")]
public static class ProjectableStringExtensions
{
	[Projectable]
	public static bool IsLessThan(this string a, string b) => a.CompareTo(b) < 0;

	[Projectable]
	public static bool IsLessOrEqualTo(this string a, string b) => a.CompareTo(b) <= 0;

	[Projectable]
	public static bool IsGreaterThan(this string a, string b) => a.CompareTo(b) > 0;

	[Projectable]
	public static bool IsGreaterOrEqualTo(this string a, string b) => a.CompareTo(b) >= 0;
}

public static class StringBuilderExtensions
{
	private const char NewLineLf = '\n';

	/// <summary>
	///     Equivalent to .AppendLine, but always uses \n instead of Environment.NewLine
	/// </summary>
	public static StringBuilder AppendLineLf(this StringBuilder sb, string? value)
	{
		sb.Append(value);
		return sb.Append(NewLineLf);
	}
}