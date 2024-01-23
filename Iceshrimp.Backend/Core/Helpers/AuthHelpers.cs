using System.Diagnostics.CodeAnalysis;
using Isopoh.Cryptography.Argon2;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AuthHelpers {
	// TODO: Implement legacy (bcrypt) hash detection
	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH")]
	public static bool ComparePassword(string password, string hash) {
		return Argon2.Verify(hash, password);
	}

	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH")]
	public static string HashPassword(string password) {
		return Argon2.Hash(password, parallelism: 4);
	}
}