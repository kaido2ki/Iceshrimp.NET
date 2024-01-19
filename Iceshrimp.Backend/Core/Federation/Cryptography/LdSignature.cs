using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class LdSignature {
	public static async Task<bool> Verify(JArray activity, string key) {
		foreach (var act in activity) {
			var options = act["https://w3id.org/security#signature"];
			if (options?.ToObject<SignatureOptions[]>() is not { Length: 1 } signatures) return false;
			var signature = signatures[0];
			if (signature.Type is not ["_:RsaSignature2017"]) return false;
			if (signature.Signature is null) return false;

			var signatureData = await GetSignatureData(act, options);
			if (signatureData is null) return false;

			var rsa = RSA.Create();
			rsa.ImportFromPem(key);
			var verify = rsa.VerifyData(signatureData, Convert.FromBase64String(signature.Signature),
			                            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			if (!verify) return false;
		}

		return true;
	}

	public static async Task<JToken> Sign(JToken data, string key, string? creator) {
		var options = new SignatureOptions {
			Created = DateTime.Now,
			Creator = creator,
			Nonce   = CryptographyHelpers.GenerateRandomHexString(16),
			Type    = ["_:RsaSignature2017"],
			Domain  = null,
		};

		var signatureData = await GetSignatureData(data, options);
		if (signatureData == null) throw new NullReferenceException("Signature data must not be null");

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		var signatureBytes = rsa.SignData(signatureData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var signature = Convert.ToBase64String(signatureBytes);

		options.Signature = signature;
		
		if (data is not JObject obj) throw new Exception();
		obj.Add("https://w3id.org/security#signature", JToken.FromObject(options));
		
		return obj;
	}

	private static Task<byte[]?> GetSignatureData(JToken data, SignatureOptions options) =>
		GetSignatureData(data, LDHelpers.Expand(JObject.FromObject(options))!);

	private static async Task<byte[]?> GetSignatureData(JToken data, JToken options) {
		if (data is not JObject inputData) return null;
		if (options is not JArray { Count: 1 } inputOptionsArray) return null;
		if (inputOptionsArray[0] is not JObject inputOptions) return null;

		inputOptions.Remove("@id");
		inputOptions.Remove("@type");
		inputOptions.Remove("https://w3id.org/security#signatureValue");

		inputData.Remove("https://w3id.org/security#signature");

		var canonicalData    = LDHelpers.Canonicalize(inputData);
		var canonicalOptions = LDHelpers.Canonicalize(inputOptions);

		var dataHash    = await DigestHelpers.Sha256Digest(canonicalData);
		var optionsHash = await DigestHelpers.Sha256Digest(canonicalOptions);

		return Encoding.UTF8.GetBytes(optionsHash + dataHash);
	}

	private class SignatureOptions {
		[J("@type")]    public required List<string> Type    { get; set; }

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