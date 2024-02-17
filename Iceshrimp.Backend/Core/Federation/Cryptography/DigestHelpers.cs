using System.Security.Cryptography;
using System.Text;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class DigestHelpers
{
	public static async Task<string> Sha256DigestAsync(string input)
	{
		var bytes = Encoding.UTF8.GetBytes(input);
		return await Sha256DigestAsync(new MemoryStream(bytes));
	}

	public static async Task<string> Sha256DigestAsync(Stream input)
	{
		var data = await SHA256.HashDataAsync(input);
		return Convert.ToHexString(data).ToLowerInvariant();
	}
}