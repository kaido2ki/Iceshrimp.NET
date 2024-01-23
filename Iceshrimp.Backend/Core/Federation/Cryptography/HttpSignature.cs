using System.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class HttpSignature {
	public static Task<bool> Verify(HttpRequest request, HttpSignatureHeader signature,
	                                IEnumerable<string> requiredHeaders, string key) {
		if (!requiredHeaders.All(signature.Headers.Contains))
			throw new ConstraintException("Request is missing required headers");

		var signingString = GenerateSigningString(signature.Headers, request.Method,
		                                          request.Path,
		                                          request.Headers);

		return VerifySignature(key, signingString, signature, request.Headers, request.Body);
	}

	//TODO: make this share code with the the regular Verify function
	public static Task<bool> Verify(this HttpRequestMessage request, string key) {
		var signatureHeader = request.Headers.GetValues("Signature").First();
		var signature       = Parse(signatureHeader);
		var signingString = GenerateSigningString(signature.Headers, request.Method.Method,
		                                          request.RequestUri!.AbsolutePath,
		                                          request.Headers.ToHeaderDictionary());

		return VerifySignature(key, signingString, signature, request.Headers.ToHeaderDictionary(),
		                       request.Content?.ReadAsStream());
	}

	private static async Task<bool> VerifySignature(string key, string signingString, HttpSignatureHeader signature,
	                                                IHeaderDictionary headers, Stream? body) {
		if (!headers.TryGetValue("date", out var date)) throw new Exception("Date header is missing");
		if (DateTime.Now - DateTime.Parse(date!) > TimeSpan.FromHours(12)) throw new Exception("Signature too old");

		//TODO: does this break for requests without a body?
		if (body != null) {
			var digest = await SHA256.HashDataAsync(body);

			//TODO: check for the SHA256= prefix instead of blindly removing the first 8 chars
			if (Convert.ToBase64String(digest) != headers["digest"].ToString().Remove(0, 8))
				throw new ConstraintException("Request digest mismatch");
		}

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(Encoding.UTF8.GetBytes(signingString), signature.Signature,
		                      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public static HttpRequestMessage Sign(this HttpRequestMessage request, IEnumerable<string> requiredHeaders,
	                                      string key, string keyId) {
		ArgumentNullException.ThrowIfNull(request.RequestUri);

		request.Headers.Date = DateTime.Now;
		request.Headers.Host = request.RequestUri.Host;

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
	                                            IHeaderDictionary requestHeaders, string? host = null) {
		var sb = new StringBuilder();

		//TODO: handle additional params, see https://github.com/Chocobozzz/node-http-signature/blob/master/lib/parser.js#L294-L310
		foreach (var header in headers) {
			sb.Append($"{header}: ");
			sb.AppendLine(header switch {
				"(request-target)" => $"{requestMethod.ToLowerInvariant()} {requestPath}",
				"host"             => $"{host ?? requestHeaders[header]}",
				_                  => requestHeaders[header]
			});
		}

		return sb.ToString()[..^1]; // remove trailing newline
	}

	private static HeaderDictionary ToHeaderDictionary(this HttpRequestHeaders headers) {
		return new HeaderDictionary(headers.ToDictionary(p => p.Key.ToLowerInvariant(),
		                                                 p => new StringValues(p.Value.ToArray())));
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