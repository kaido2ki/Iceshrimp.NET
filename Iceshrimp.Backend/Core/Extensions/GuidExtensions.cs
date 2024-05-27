namespace Iceshrimp.Backend.Core.Extensions;

public static class GuidExtensions
{
	public static string ToStringLower(this Guid guid) => guid.ToString().ToLowerInvariant();
	public static string ToStringLower(this Ulid ulid) => ulid.ToString().ToLowerInvariant();
}