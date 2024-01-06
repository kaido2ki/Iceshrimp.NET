using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public class HttpSignature {
	public readonly string KeyId;
	private readonly string _algo;
	private readonly byte[] _signature;
	private readonly byte[] _signatureData;

	public HttpSignature(HttpRequest request, IEnumerable<string> requiredHeaders) {
		if (!request.Headers.TryGetValue("signature", out var sigHeader))
			throw new ConstraintException("Signature string is missing the signature header");

		//TODO: does this break for requests without a body?
		var digest = SHA256.HashDataAsync(request.BodyReader.AsStream()).AsTask();

		var sig = sigHeader.ToString().Split(",")
		                 .Select(s => s.Split('='))
		                 .ToDictionary(p => p[0], p => (p[1] + new string('=', p.Length - 2)).Trim('"'));
		
		var signatureBase64 = sig["signature"] ?? throw new ConstraintException("Signature string is missing the signature field");

		KeyId     = sig["keyId"] ?? throw new ConstraintException("Signature string is missing the keyId field");
		_algo      = sig["algorithm"] ?? throw new ConstraintException("Signature string is missing the algorithm field");
		_signature = Convert.FromBase64String(signatureBase64);

		var headers = sig["headers"].Split(" ") ??
		              throw new ConstraintException("Signature data is missing the headers field");

		var sb = new StringBuilder();
		if (!requiredHeaders.All(headers.Contains))
			throw new ConstraintException("Request is missing required headers");

		//TODO: handle additional params, see https://github.com/Chocobozzz/node-http-signature/blob/master/lib/parser.js#L294-L310
		foreach (var header in headers) {
			sb.Append($"{header}: ");
			sb.AppendLine(header switch {
				"(request-target)" => $"{request.Method.ToLower()} {request.Path}",
				_                  => request.Headers[header]
			});
		}

		_signatureData = Encoding.UTF8.GetBytes(sb.ToString(0, sb.Length - 1)); // remove trailing newline

		//TODO: this definitely breaks if there's no body
		//TODO: check for the SHA256= prefix instead of blindly removing the first 8 chars
		if (Convert.ToBase64String(digest.Result) != request.Headers["digest"].ToString().Remove(0, 8)) 
			throw new ConstraintException("Request digest mismatch");
	}

	public bool Verify(string key) {
		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(_signatureData, _signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}
}