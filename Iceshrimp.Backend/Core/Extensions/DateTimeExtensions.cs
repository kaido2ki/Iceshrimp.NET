namespace Iceshrimp.Backend.Core.Extensions;

public static class DateTimeExtensions
{
	public static string ToStringIso8601Like(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK");
	}
}