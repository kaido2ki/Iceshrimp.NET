using System.Net;
using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class LdSignature {
	public static Task<bool> Verify(JArray activity, string key) {
		if (activity.ToArray() is not [JObject obj])
			throw new CustomException(HttpStatusCode.UnprocessableEntity, "Invalid activity");
		return Verify(obj, key);
	}

	public static async Task<bool> Verify(JObject activity, string key) {
		var options = activity["https://w3id.org/security#signature"];
		if (options?.ToObject<SignatureOptions[]>() is not { Length: 1 } signatures) return false;
		var signature = signatures[0];
		if (signature.Type is not ["_:RsaSignature2017"]) return false;
		if (signature.Signature is null) return false;

		var signatureData = await GetSignatureData(activity, options);
		if (signatureData is null) return false;

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(signatureData, Convert.FromBase64String(signature.Signature),
		                      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public static Task<JObject> Sign(JArray activity, string key, string? creator) {
		if (activity.ToArray() is not [JObject obj])
			throw new CustomException(HttpStatusCode.UnprocessableEntity, "Invalid activity");
		return Sign(obj, key, creator);
	}

	public static async Task<JObject> Sign(JObject activity, string key, string? creator) {
		var options = new SignatureOptions {
			Created = DateTime.Now,
			Creator = creator,
			Nonce   = CryptographyHelpers.GenerateRandomHexString(16),
			Type    = ["_:RsaSignature2017"],
			Domain  = null
		};

		var signatureData = await GetSignatureData(activity, options);
		if (signatureData == null)
			throw new CustomException(HttpStatusCode.Forbidden, "Signature data must not be null");

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		var signatureBytes = rsa.SignData(signatureData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var signature      = Convert.ToBase64String(signatureBytes);

		options.Signature = signature;

		activity.Add("https://w3id.org/security#signature", JToken.FromObject(options));

		return LdHelpers.Expand(activity)?[0] as JObject ??
		       throw new CustomException(HttpStatusCode.UnprocessableEntity, "Failed to expand signed activity");
	}

	private static Task<byte[]?> GetSignatureData(JToken data, SignatureOptions options) {
		return GetSignatureData(data, LdHelpers.Expand(JObject.FromObject(options))!);
	}

	private static async Task<byte[]?> GetSignatureData(JToken data, JToken options) {
		if (data is not JObject inputData) return null;
		if (options is not JArray { Count: 1 } inputOptionsArray) return null;
		if (inputOptionsArray[0] is not JObject inputOptions) return null;

		inputOptions.Remove("@id");
		inputOptions.Remove("@type");
		inputOptions.Remove("https://w3id.org/security#signatureValue");

		inputData.Remove("https://w3id.org/security#signature");

		var canonicalData    = LdHelpers.Canonicalize(inputData);
		var canonicalOptions = LdHelpers.Canonicalize(inputOptions);

		var dataHash    = await DigestHelpers.Sha256Digest(canonicalData);
		var optionsHash = await DigestHelpers.Sha256Digest(canonicalOptions);

		return Encoding.UTF8.GetBytes(optionsHash + dataHash);
	}

	private class SignatureOptions {
		[J("@type")] public required List<string> Type { get; set; }

		[J("https://w3id.org/security#signatureValue")]
		[JC(typeof(VC))]
		public string? Signature { get; set; }

		[J("http://purl.org/dc/terms/creator", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		public string? Creator { get; set; }

		[J("https://w3id.org/security#nonce", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		public string? Nonce { get; set; }

		[J("https://w3id.org/security#domain", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		public string? Domain { get; set; }

		[J("https://w3id.org/security#created", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		//FIXME: is this valid? it should output datetime in ISO format
		public DateTime? Created { get; set; }
	}
}