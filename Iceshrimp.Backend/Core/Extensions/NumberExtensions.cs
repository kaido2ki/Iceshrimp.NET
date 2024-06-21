using System.Text;

namespace Iceshrimp.Backend.Core.Extensions;

public static class NumberExtensions
{
	private const string Base36Charset = "0123456789abcdefghijklmnopqrstuvwxyz";

	public static string ToBase36(this long input)
	{
		if (input == 0) return "0";
		var result = new StringBuilder();

		while (input > 0)
		{
			result.Insert(0, Base36Charset[(int)(input % 36)]);
			input /= 36;
		}

		return result.ToString();
	}

	public static string ToBase36(this int input)
	{
		if (input == 0) return "0";
		var result = new StringBuilder();

		while (input >= 0)
		{
			result.Insert(0, Base36Charset[input % 36]);
			input /= 36;
		}

		return result.ToString();
	}

	public static string ToDurationDisplayString(this long input)
	{
		return input switch
		{
			< 1000           => $"{input} ms",
			< 1000 * 60      => $"{Math.Round(input / 1000d / 60d, 2)} s",
			< 1000 * 60 * 60 => $"{Math.Round(input / 60000d / 60d, 2)} m",
			_                => $"{Math.Round(input / 60000d / 60d / 60d, 2)} h"
		};
	}
	
	public static string ToDurationDisplayString(this int input)
	{
		return input switch
		{
			< 1000           => $"{input} ms",
			< 1000 * 60      => $"{Math.Round(input / 1000d / 60d, 2)} s",
			< 1000 * 60 * 60 => $"{Math.Round(input / 60000d / 60d, 2)} m",
			_                => $"{Math.Round(input / 60000d / 60d / 60d, 2)} h"
		};
	}
}