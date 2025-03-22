namespace Iceshrimp.Backend.Core.Extensions;

public static class TimeSpanExtensions
{
	private static readonly long Seconds = TimeSpan.FromMinutes(1).Ticks;
	private static readonly long Minutes = TimeSpan.FromHours(1).Ticks;
	private static readonly long Hours   = TimeSpan.FromDays(1).Ticks;

	public static long GetTotalMilliseconds(this TimeSpan timeSpan) => Convert.ToInt64(timeSpan.TotalMilliseconds);

	public static string ToDisplayString(this TimeSpan timeSpan, bool singleNumber = true)
	{
		if (timeSpan.Ticks < Seconds)
		{
			var seconds = (int)timeSpan.TotalSeconds;
			return seconds == 1 ? singleNumber ? "1 second" : "second" : $"{seconds} seconds";
		}

		if (timeSpan.Ticks < Minutes)
		{
			var minutes = (int)timeSpan.TotalMinutes;
			return minutes == 1 ? singleNumber ? "1 minute" : "minute" : $"{minutes} minutes";
		}

		if (timeSpan.Ticks < Hours)
		{
			var hours = (int)timeSpan.TotalHours;
			return hours == 1 ? singleNumber ? "1 hour" : "hour" : $"{hours} hours";
		}

		var days = (int)timeSpan.TotalDays;
		return days == 1 ? singleNumber ? "1 day" : "day" : $"{days} days";
	}
}
