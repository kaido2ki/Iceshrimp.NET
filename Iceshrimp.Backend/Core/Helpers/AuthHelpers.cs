using System.Diagnostics.CodeAnalysis;
using Isopoh.Cryptography.Argon2;

namespace Iceshrimp.Backend.Core.Helpers;

public static class AuthHelpers {
	// TODO: Implement legacy hash detection
	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH")]
	public static bool ComparePassword(string password, string hash) {
		return Argon2.Verify(hash, password);
	}
}