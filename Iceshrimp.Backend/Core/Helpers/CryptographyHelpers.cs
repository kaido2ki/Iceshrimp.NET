using System.Security.Cryptography;

namespace Iceshrimp.Backend.Core.Helpers;

public static class CryptographyHelpers
{
	public static string GenerateRandomHexString(int length)
		=> RandomNumberGenerator.GetHexString(length, true);

	public static string GenerateRandomString(int length, Charset charset = Charset.AlphaNum)
		=> RandomNumberGenerator.GetString(GetCharset(charset), length);

	public enum Charset
	{
		AlphaNum,
		AlphaNumLower,
		CrockfordBase32,
		CrockfordBase32Lower
	}

	private static string GetCharset(Charset charset) => charset switch
	{
		Charset.AlphaNum             => AlphaNumCharset,
		Charset.AlphaNumLower        => AlphaNumLowerCharset,
		Charset.CrockfordBase32      => CrockfordBase32Charset,
		Charset.CrockfordBase32Lower => CrockfordBase32LowerCharset,
		_                            => throw new ArgumentOutOfRangeException(nameof(charset), charset, null)
	};

	private const string AlphaNumCharset             = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
	private const string AlphaNumLowerCharset        = "abcdefghijklmnopqrstuvwxyz0123456789";
	private const string CrockfordBase32Charset      = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
	private const string CrockfordBase32LowerCharset = "0123456789abcdefghjkmnpqrstvwxyz";
}