using System.Text.RegularExpressions;

namespace Iceshrimp.Backend.Core.Helpers;

public static class EfHelpers
{
	public static string EscapeLikeQuery(string input) => input.Replace(@"\", @"\\")
	                                                           .Replace("%", @"\%")
	                                                           .Replace("_", @"\_")
	                                                           .Replace("^", @"\^")
	                                                           .Replace("[", @"\[")
	                                                           .Replace("]", @"\]");

	public static string EscapeRegexQuery(string input) =>
		new Regex(@"([!$()*+.:<=>?[\\\]^{|}-])").Replace(input, "\\$1");
}