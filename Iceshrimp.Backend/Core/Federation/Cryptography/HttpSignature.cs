using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.Extensions.Primitives;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class HttpSignature {
	public static async Task<bool> VerifyAsync(HttpRequest request, HttpSignatureHeader signature,
	                                           IEnumerable<string> requiredHeaders, string key) {
		if (!requiredHeaders.All(signature.Headers.Contains))
			throw new GracefulException(HttpStatusCode.Forbidden, "Request is missing required headers");

		var signingString = GenerateSigningString(signature.Headers, request.Method,
		                                          request.Path,
		                                          request.Headers);

		if (request.Body.CanSeek) request.Body.Position = 0;
		return await VerifySignatureAsync(key, signingString, signature, request.Headers,
		                                  request.ContentLength > 0 ? request.Body : null);
	}

	public static async Task<bool> VerifyAsync(this HttpRequestMessage request, string key) {
		var signatureHeader = request.Headers.GetValues("Signature").First();
		var signature       = Parse(signatureHeader);
		var signingString = GenerateSigningString(signature.Headers, request.Method.Method,
		                                          request.RequestUri!.AbsolutePath,
		                                          request.Headers.ToHeaderDictionary());

		Stream? body = null;

		if (request.Content != null) body = await request.Content.ReadAsStreamAsync();

		return await VerifySignatureAsync(key, signingString, signature, request.Headers.ToHeaderDictionary(), body);
	}

	private static async Task<bool> VerifySignatureAsync(string key, string signingString,
	                                                     HttpSignatureHeader signature,
	                                                     IHeaderDictionary headers, Stream? body) {
		if (!headers.TryGetValue("date", out var date))
			throw new GracefulException(HttpStatusCode.Forbidden, "Date header is missing");
		if (DateTime.Now - DateTime.Parse(date!) > TimeSpan.FromHours(12))
			throw new GracefulException(HttpStatusCode.Forbidden, "Request signature too old");

		if (body is { Length: > 0 }) {
			if (body.Position != 0)
				body.Position = 0;
			var digest = await SHA256.HashDataAsync(body);
			body.Position = 0;

			//TODO: check for the SHA-256= prefix instead of blindly removing the first 8 chars
			if (Convert.ToBase64String(digest) != headers["digest"].ToString().Remove(0, 8))
				return false;
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
		var sig = header.Split(",")
		                .Select(s => s.Split('='))
		                .ToDictionary(p => p[0], p => (p[1] + new string('=', p.Length - 2)).Trim('"'));

		//TODO: these fail if the dictionary doesn't contain the key, use TryGetValue instead
		var signatureBase64 = sig["signature"] ??
		                      throw new GracefulException(HttpStatusCode.Forbidden,
		                                                  "Signature string is missing the signature field");
		var headers = sig["headers"].Split(" ") ??
		              throw new GracefulException(HttpStatusCode.Forbidden,
		                                          "Signature data is missing the headers field");

		var keyId = sig["keyId"] ??
		            throw new GracefulException(HttpStatusCode.Forbidden,
		                                        "Signature string is missing the keyId field");

		//TODO: this should fallback to sha256
		var algo = sig["algorithm"] ??
		           throw new GracefulException(HttpStatusCode.Forbidden,
		                                       "Signature string is missing the algorithm field");

		var signature = Convert.FromBase64String(signatureBase64);

		return new HttpSignatureHeader(keyId, algo, signature, headers);
	}

	public class HttpSignatureHeader(string keyId, string algo, byte[] signature, IEnumerable<string> headers) {
		public readonly string              Algo      = algo;
		public readonly IEnumerable<string> Headers   = headers;
		public readonly string              KeyId     = keyId;
		public readonly byte[]              Signature = signature;
	}
}