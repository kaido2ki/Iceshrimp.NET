using OtpNet;

namespace Iceshrimp.Backend.Core.Helpers;

public static class TotpHelper
{
	private static readonly VerificationWindow VerificationWindow = new(1, 1);

	public static bool Validate(string secret, string totp)
		=> new Totp(Base32Encoding.ToBytes(secret)).VerifyTotp(totp, out _, VerificationWindow);

	public static string GenerateSecret()
		=> Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
}
