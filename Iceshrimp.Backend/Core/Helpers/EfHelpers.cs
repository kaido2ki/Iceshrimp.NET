namespace Iceshrimp.Backend.Core.Helpers;

public static class EfHelpers {
	public static string EscapeLikeQuery(string input) => input.Replace(@"\", @"\\")
	                                                           .Replace("%", @"\%")
	                                                           .Replace("_", @"\_")
	                                                           .Replace("^", @"\^")
	                                                           .Replace("[", @"\[")
	                                                           .Replace("]", @"\]");
}