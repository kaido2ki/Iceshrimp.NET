using Iceshrimp.Backend.Core.Extensions;

namespace Iceshrimp.Backend.Core.Helpers;

public static class IdHelpers
{
	private const long Time2000 = 946684800000;

	public static string GenerateSnowflakeId(DateTime? createdAt = null)
	{
		if (createdAt is { Kind: not DateTimeKind.Utc })
			createdAt = createdAt.Value.ToUniversalTime();

		createdAt ??= DateTime.UtcNow;

		// We want to use a charset with a power-of-two amount of possible characters for optimal CSPRNG performance.
		var random    = CryptographyHelpers.GenerateRandomString(8, CryptographyHelpers.Charset.CrockfordBase32Lower);
		var now       = createdAt.Value.Subtract(DateTime.UnixEpoch).GetTotalMilliseconds();
		var time      = Math.Max(now - Time2000, 0);
		var timestamp = time.ToBase36().PadLeft(8, '0');
		return timestamp + random;
	}
}