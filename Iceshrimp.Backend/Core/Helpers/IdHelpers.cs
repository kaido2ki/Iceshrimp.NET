using Iceshrimp.Backend.Core.Extensions;
using Visus.Cuid;

namespace Iceshrimp.Backend.Core.Helpers;

public static class IdHelpers {
	private const long Time2000 = 946684800000;

	public static string GenerateSlowflakeId(DateTime? createdAt = null) {
		if (createdAt?.Kind is not null and not DateTimeKind.Utc)
			createdAt = createdAt.Value.ToUniversalTime();

		createdAt ??= DateTime.UtcNow;

		var cuid      = new Cuid2(8);
		var now       = (long)createdAt.Value.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
		var time      = Math.Max(now - Time2000, 0);
		var timestamp = time.ToBase36().PadLeft(8, '0');
		return timestamp + cuid;
	}
}