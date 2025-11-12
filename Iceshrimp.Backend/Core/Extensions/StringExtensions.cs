using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using EntityFrameworkCore.Projectables;

namespace Iceshrimp.Backend.Core.Extensions;

public static class StringExtensions
{
	private static readonly IdnMapping IdnMapping = new();

	extension(string? s1)
	{
		public bool EqualsInvariant(string? s2) =>
			string.Equals(s1, s2, StringComparison.InvariantCulture);

		public bool EqualsIgnoreCase(string s2) =>
			string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase);
	}

	extension(string target)
	{
		public string Truncate(int maxLength)
		{
			return target[..Math.Min(target.Length, maxLength)];
		}

		public string TruncateEllipsis(int maxLength)
		{
			if (target.Length <= maxLength) return target;
			return target[..(maxLength-3)] + "...";
		}

		private string ToPunycode()
		{
			return target.Length > 0 ? IdnMapping.GetAscii(target) : target;
		}

		public string ToPunycodeLower()
		{
			return ToPunycode(target).ToLowerInvariant();
		}

		public string FromPunycode()
		{
			return IdnMapping.GetUnicode(target);
		}

		public string ToTitleCase() => target switch
		{
			null => throw new ArgumentNullException(nameof(target)),
			""   => throw new ArgumentException(@$"{nameof(target)} cannot be empty", nameof(target)),
			_    => string.Concat(target[0].ToString().ToUpper(), target.AsSpan(1))
		};

		public string UrlEncode() => UrlEncoder.Default.Encode(target);
	}
}

[SuppressMessage("ReSharper", "StringCompareToIsCultureSpecific", Justification = "SQL")]
[SuppressMessage("ReSharper", "ConvertToExtensionBlock", Justification = "Projectables")]
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

	extension(StringBuilder sb)
	{
		/// <summary>
		///     Equivalent to .AppendLine, but always uses \n instead of Environment.NewLine
		/// </summary>
		public StringBuilder AppendLineLf(string? value)
		{
			sb.Append(value);
			return sb.Append(NewLineLf);
		}
	}
}