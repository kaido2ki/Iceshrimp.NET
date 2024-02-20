using System.Security.Cryptography;

namespace Iceshrimp.Backend.Core.Helpers;

public static class CryptographyHelpers
{
	private const string AlphaNumCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
	public static string GenerateRandomString(int length) => RandomNumberGenerator.GetString(AlphaNumCharset, length);
	public static string GenerateRandomHexString(int length) => RandomNumberGenerator.GetHexString(length, true);
}