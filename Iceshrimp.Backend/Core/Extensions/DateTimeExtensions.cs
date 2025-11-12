namespace Iceshrimp.Backend.Core.Extensions;

public static class DateTimeExtensions
{
	extension(DateTime dateTime)
	{
		public string ToStringIso8601Like() => dateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK");
		public string ToDisplayString()     => dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm");
		public string ToDisplayStringTz()   => dateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'sszz");
	}
}