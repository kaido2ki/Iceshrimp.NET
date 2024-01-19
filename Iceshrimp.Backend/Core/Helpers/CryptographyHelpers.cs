using System.Security.Cryptography;

namespace Iceshrimp.Backend.Core.Helpers;

public static class CryptographyHelpers {
	public static string GenerateRandomString(int length) =>
		Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));

	public static string GenerateRandomHexString(int length) =>
		Convert.ToHexString(RandomNumberGenerator.GetBytes(length)).ToLowerInvariant();
}