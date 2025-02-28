namespace Iceshrimp.Backend.Core.Extensions;

public static class DateTimeExtensions
{
	public static string ToStringIso8601Like(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK");
	}

	public static string ToDisplayString(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm");
	}

	public static string ToDisplayStringTz(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'sszz");
	}
}