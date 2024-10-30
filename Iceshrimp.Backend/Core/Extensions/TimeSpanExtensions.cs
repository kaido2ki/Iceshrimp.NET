namespace Iceshrimp.Backend.Core.Extensions;

public static class TimeSpanExtensions
{
	public static long GetTotalMilliseconds(this TimeSpan timeSpan) => Convert.ToInt64(timeSpan.TotalMilliseconds);
}