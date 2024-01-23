using System.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class HttpSignature {
	public static async Task<bool> Verify(HttpRequest request, HttpSignatureHeader signature,
	                                      IEnumerable<string> requiredHeaders, string key) {
		if (!requiredHeaders.All(signature.Headers.Contains))
			throw new ConstraintException("Request is missing required headers");
		
		//TODO: verify date header exists and is set to something the last 12 hours

		var signingString = GenerateSigningString(signature.Headers, request.Method,
		                                          request.Path,
		                                          request.Headers);

		//TODO: does this break for requests without a body?
		var digest = await SHA256.HashDataAsync(request.BodyReader.AsStream());

		//TODO: this definitely breaks if there's no body
		//TODO: check for the SHA256= prefix instead of blindly removing the first 8 chars
		if (Convert.ToBase64String(digest) != request.Headers["digest"].ToString().Remove(0, 8))
			throw new ConstraintException("Request digest mismatch");

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(Encoding.UTF8.GetBytes(signingString), signature.Signature,
		                      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public static bool VerifySign(this HttpRequestMessage request, string key) {
		var signatureHeader = request.Headers.GetValues("Signature").First();
		var signature       = Parse(signatureHeader);
		var signingString = GenerateSigningString(signature.Headers, request.Method.Method,
		                                          request.RequestUri!.AbsolutePath,
		                                          request.Headers.ToHeaderDictionary());

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(Encoding.UTF8.GetBytes(signingString), signature.Signature,
		                      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public static HttpRequestMessage Sign(this HttpRequestMessage request, IEnumerable<string> requiredHeaders,
	                                      string key, string keyId) {
		ArgumentNullException.ThrowIfNull(request.RequestUri);

		request.Headers.Date = DateTime.Now;
		var requiredHeadersEnum = requiredHeaders.ToList();
		var signingString = GenerateSigningString(requiredHeadersEnum, request.Method.Method,
		                                          request.RequestUri.AbsolutePath,
		                                          request.Headers.ToHeaderDictionary());
		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(signingString),
		                                  HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var signatureBase64 = Convert.ToBase64String(signatureBytes);
		var signatureHeader = $"""
		                       keyId="{keyId}",headers="{string.Join(' ', requiredHeadersEnum)}",algorithm="hs2019",signature="{signatureBase64}"
		                       """;


		request.Headers.Add("Signature", signatureHeader);
		return request;
	}

	private static string GenerateSigningString(IEnumerable<string> headers, string requestMethod, string requestPath,
	                                            IHeaderDictionary requestHeaders) {
		var sb = new StringBuilder();

		//TODO: handle additional params, see https://github.com/Chocobozzz/node-http-signature/blob/master/lib/parser.js#L294-L310
		foreach (var header in headers) {
			sb.Append($"{header}: ");
			sb.AppendLine(header switch {
				"(request-target)" => $"{requestMethod.ToLowerInvariant()} {requestPath}",
				_                  => requestHeaders[header]
			});
		}

		return sb.ToString()[..^1]; // remove trailing newline
	}

	private static HeaderDictionary ToHeaderDictionary(this HttpRequestHeaders headers) {
		return new HeaderDictionary(headers.ToDictionary(p => p.Key, p => new StringValues(p.Value.ToArray())));
	}

	public static HttpSignatureHeader Parse(string header) {
		//if (!request.Headers.TryGetValue("signature", out var sigHeader))
		// throw new ConstraintException("Signature string is missing the signature header");

		var sig = header.Split(",")
		                .Select(s => s.Split('='))
		                .ToDictionary(p => p[0], p => (p[1] + new string('=', p.Length - 2)).Trim('"'));

		//TODO: these fail if the dictionary doesn't contain the key, use TryGetValue instead
		var signatureBase64 = sig["signature"] ??
		                      throw new ConstraintException("Signature string is missing the signature field");
		var headers = sig["headers"].Split(" ") ??
		              throw new ConstraintException("Signature data is missing the headers field");

		var keyId = sig["keyId"] ?? throw new ConstraintException("Signature string is missing the keyId field");

		//TODO: this should fallback to sha256
		var algo = sig["algorithm"] ?? throw new ConstraintException("Signature string is missing the algorithm field");

		var signature = Convert.FromBase64String(signatureBase64);

		return new HttpSignatureHeader(keyId, algo, signature, headers);
	}

	public class HttpSignatureHeader(string keyId, string algo, byte[] signature, IEnumerable<string> headers) {
		public readonly string              KeyId     = keyId;
		public readonly string              Algo      = algo;
		public readonly byte[]              Signature = signature;
		public readonly IEnumerable<string> Headers   = headers;
	}
}