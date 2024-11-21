using OtpNet;

namespace Iceshrimp.Backend.Core.Helpers;

public static class TotpHelper
{
	public static bool Validate(string secret, string totp)
		=> new Totp(Base32Encoding.ToBytes(secret)).VerifyTotp(totp, out _);

	public static string GenerateSecret()
		=> Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
}