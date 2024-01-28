using System.Security.Cryptography;
using System.Text;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class DigestHelpers {
	public static async Task<string> Sha256DigestAsync(string input) {
		var bytes = Encoding.UTF8.GetBytes(input);
		var data  = await SHA256.HashDataAsync(new MemoryStream(bytes));
		return Convert.ToHexString(data).ToLowerInvariant();
	}
}