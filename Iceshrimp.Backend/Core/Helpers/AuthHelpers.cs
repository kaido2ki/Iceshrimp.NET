using System.Diagnostics.CodeAnalysis;
using BC = BCrypt.Net.BCrypt;
using Isopoh.Cryptography.Argon2;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AuthHelpers
{
	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH",
	                 MessageId = "type: System.UInt64[]; size: 65MB",
	                 Justification = "Argon2 is memory-heavy by design")]
	public static bool ComparePassword(string password, string hash)
	{
		if (isLegacyHash(hash))
		{
			return BC.Verify(password, hash);
		}

		return Argon2.Verify(hash, password);
	}

	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH",
	                 MessageId = "type: System.UInt64[]; size: 65MB",
	                 Justification = "Argon2 is memory-heavy by design")]
	public static string HashPassword(string password)
	{
		return Argon2.Hash(password, parallelism: 4);
	}

	public static bool isLegacyHash(string hash)
	{
		return hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$") || hash.StartsWith("$2x$");
	}
}