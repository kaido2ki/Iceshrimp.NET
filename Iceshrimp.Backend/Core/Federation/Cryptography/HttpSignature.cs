using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.Extensions.Primitives;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class HttpSignature
{
	public static async Task<bool> VerifyAsync(
		HttpRequest request, HttpSignatureHeader signature,
		IEnumerable<string> requiredHeaders, string key
	)
	{
		if (!requiredHeaders.All(signature.Headers.Contains))
			throw new GracefulException(HttpStatusCode.Forbidden, "Request is missing required headers");

		var signingString =
			GenerateSigningString(signature.Headers, request.Method, request.Path, request.Headers, null, signature);

		if (request.Body.CanSeek) request.Body.Position = 0;
		return await VerifySignatureAsync(key, signingString, signature, request.Headers,
		                                  request.ContentLength > 0 ? request.Body : null);
	}

	public static async Task<bool> VerifyAsync(this HttpRequestMessage request, string key)
	{
		var signatureHeader = request.Headers.GetValues("Signature").First();
		var signature       = Parse(signatureHeader);
		var signingString = GenerateSigningString(signature.Headers, request.Method.Method,
		                                          request.RequestUri!.AbsolutePath,
		                                          request.Headers.ToHeaderDictionary(),
		                                          null, signature);

		Stream? body = null;

		if (request.Content != null) body = await request.Content.ReadAsStreamAsync();

		return await VerifySignatureAsync(key, signingString, signature, request.Headers.ToHeaderDictionary(), body);
	}

	public static async Task<bool> VerifySignatureAsync(
		string key, string signingString,
		HttpSignatureHeader signature,
		IHeaderDictionary headers, Stream? body
	)
	{
		var created     = signature.Created;
		var datePresent = headers.TryGetValue("date", out var date);
		if (created == null && !datePresent)
			throw new GracefulException(HttpStatusCode.Forbidden, "Neither date nor (created) are present, refusing");

		var dateCheck = datePresent && DateTime.Now - DateTime.Parse(date!) > TimeSpan.FromHours(12);
		var createdCheck = created != null &&
		                   DateTime.UtcNow - (DateTime.UnixEpoch + TimeSpan.FromSeconds(long.Parse(created))) >
		                   TimeSpan.FromHours(12);

		if (dateCheck || createdCheck)
			throw new GracefulException(HttpStatusCode.Forbidden, "Request signature is too old");

		var expiryCheck = signature.Expires != null &&
		                  DateTime.UtcNow - (DateTime.UnixEpoch + TimeSpan.FromSeconds(long.Parse(signature.Expires))) >
		                  TimeSpan.Zero;

		if (expiryCheck)
			throw new GracefulException(HttpStatusCode.Forbidden, "Request signature is expired");

		if (body is { Length: > 0 })
		{
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

	public static HttpRequestMessage Sign(
		this HttpRequestMessage request, IEnumerable<string> requiredHeaders,
		string key, string keyId
	)
	{
		ArgumentNullException.ThrowIfNull(request.RequestUri);

		request.Headers.Date = DateTimeOffset.UtcNow;
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

	public static string GenerateSigningString(
		IEnumerable<string> headers, string requestMethod, string requestPath,
		IHeaderDictionary requestHeaders, string? host = null, HttpSignatureHeader? signature = null
	)
	{
		var sb = new StringBuilder();

		foreach (var header in headers)
		{
			sb.Append($"{header}: ");
			sb.AppendLineLf(header switch
			{
				"(request-target)" => $"{requestMethod.ToLowerInvariant()} {requestPath}",
				"(created)"        => signature?.Created ?? throw new Exception("Signature is missing created param"),
				"(keyid)"          => signature?.KeyId ?? throw new Exception("Signature is missing keyId param"),
				"(algorithm)"      => signature?.Algo ?? throw new Exception("Signature is missing algorithm param"),
				"(expires)"        => signature?.Expires ?? throw new Exception("Signature is missing expires param"),
				"(opaque)"         => signature?.Opaque ?? throw new Exception("Signature is missing opaque param"),
				"host"             => $"{host ?? requestHeaders[header]}",
				_                  => string.Join(", ", requestHeaders[header].AsEnumerable())
			});
		}

		return sb.ToString()[..^1]; // remove trailing newline
	}

	private static HeaderDictionary ToHeaderDictionary(this HttpRequestHeaders headers)
	{
		return new HeaderDictionary(headers.ToDictionary(p => p.Key.ToLowerInvariant(),
		                                                 p => new StringValues(p.Value.ToArray())));
	}

	public static HttpSignatureHeader Parse(string header)
	{
		var sig = header.Split(",")
		                .Select(s => s.Split('='))
		                .ToDictionary(p => p[0], p => (p[1] + new string('=', p.Length - 2)).Trim('"'));

		if (!sig.TryGetValue("signature", out var signatureBase64))
			throw GracefulException.Forbidden("Signature string is missing the signature field");

		if (!sig.TryGetValue("headers", out var headers))
			throw GracefulException.Forbidden("Signature string is missing the headers field");

		if (!sig.TryGetValue("keyId", out var keyId))
			throw GracefulException.Forbidden("Signature string is missing the keyId field");

		//TODO: this should fallback to sha256
		if (!sig.TryGetValue("algorithm", out var algo))
			throw GracefulException.Forbidden("Signature string is missing the algorithm field");

		sig.TryGetValue("created", out var created);
		sig.TryGetValue("expires", out var expires);
		sig.TryGetValue("opaque", out var opaque);

		var signature = Convert.FromBase64String(signatureBase64);

		return new HttpSignatureHeader(keyId, algo, signature, headers.Split(" "), created, expires, opaque);
	}

	public class HttpSignatureHeader(
		string keyId,
		string algo,
		byte[] signature,
		IEnumerable<string> headers,
		string? created,
		string? expires,
		string? opaque
	)
	{
		public readonly string              Algo      = algo;
		public readonly string?             Created   = created;
		public readonly string?             Expires   = expires;
		public readonly IEnumerable<string> Headers   = headers;
		public readonly string              KeyId     = keyId;
		public readonly string?             Opaque    = opaque;
		public readonly byte[]              Signature = signature;
	}
}