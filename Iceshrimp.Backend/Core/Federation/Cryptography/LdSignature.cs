using System.Net;
using System.Security.Cryptography;
using System.Text;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.ActivityStreams;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using J = Newtonsoft.Json.JsonPropertyAttribute;
using JC = Newtonsoft.Json.JsonConverterAttribute;
using VC = Iceshrimp.Backend.Core.Federation.ActivityStreams.Types.ValueObjectConverter;

namespace Iceshrimp.Backend.Core.Federation.Cryptography;

public static class LdSignature
{
	public static Task<bool> VerifyAsync(JArray activity, JArray rawActivity, string key, string? keyId = null)
	{
		if (activity.ToArray() is not [JObject obj])
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Invalid activity");
		if (rawActivity.ToArray() is not [JObject rawObj])
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Invalid activity");
		return VerifyAsync(obj, rawObj, key, keyId);
	}

	public static async Task<bool> VerifyAsync(JObject activity, JObject rawActivity, string key, string? keyId = null)
	{
		var options    = activity[$"{Constants.W3IdSecurityNs}#signature"];
		var rawOptions = rawActivity[$"{Constants.W3IdSecurityNs}#signature"];
		if (rawOptions is null) return false;
		if (options?.ToObject<SignatureOptions[]>() is not { Length: 1 } signatures) return false;
		var signature = signatures[0];
		if (signature.Type is not ["_:RsaSignature2017"]) return false;
		if (signature.Signature is null) return false;
		if (keyId != null && signature.Creator?.Id != keyId)
			throw new Exception("Creator doesn't match actor keyId");

		var signatureData = await GetSignatureDataAsync(rawActivity, rawOptions);
		if (signatureData is null) return false;

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		return rsa.VerifyData(signatureData, Convert.FromBase64String(signature.Signature),
		                      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
	}

	public static Task<JObject> SignAsync(JArray activity, string key, string? creator)
	{
		if (activity.ToArray() is not [JObject obj])
			throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Invalid activity");
		return SignAsync(obj, key, creator);
	}

	public static async Task<JObject> SignAsync(JObject activity, string key, string? creator)
	{
		var options = new SignatureOptions
		{
			Created = DateTime.Now,
			Creator = new ASObjectBase(creator),
			Nonce   = CryptographyHelpers.GenerateRandomHexString(16),
			Type    = ["_:RsaSignature2017"],
			Domain  = null
		};

		var signatureData = await GetSignatureDataAsync(activity, options);
		if (signatureData == null)
			throw new GracefulException(HttpStatusCode.Forbidden, "Signature data must not be null");

		var rsa = RSA.Create();
		rsa.ImportFromPem(key);
		var signatureBytes = rsa.SignData(signatureData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		var signature      = Convert.ToBase64String(signatureBytes);

		options.Signature = signature;

		activity.Add($"{Constants.W3IdSecurityNs}#signature", JToken.FromObject(options));

		return LdHelpers.Expand(activity)?[0] as JObject ??
		       throw new GracefulException(HttpStatusCode.UnprocessableEntity, "Failed to expand signed activity");
	}

	private static Task<byte[]?> GetSignatureDataAsync(JToken data, SignatureOptions options)
	{
		return GetSignatureDataAsync(data, LdHelpers.Expand(JObject.FromObject(options))!);
	}

	private static async Task<byte[]?> GetSignatureDataAsync(JToken data, JToken options)
	{
		if (data is not JObject inputData) return null;
		if (options is not JArray { Count: 1 } inputOptionsArray) return null;
		if (inputOptionsArray[0] is not JObject inputOptions) return null;

		inputOptions.Remove("@id");
		inputOptions.Remove("@type");
		inputOptions.Remove($"{Constants.W3IdSecurityNs}#signatureValue");

		inputData.Remove($"{Constants.W3IdSecurityNs}#signature");

		var canonicalData    = LdHelpers.Canonicalize(inputData);
		var canonicalOptions = LdHelpers.Canonicalize(inputOptions);

		var dataHash    = await DigestHelpers.Sha256DigestAsync(canonicalData);
		var optionsHash = await DigestHelpers.Sha256DigestAsync(canonicalOptions);

		return Encoding.UTF8.GetBytes(optionsHash + dataHash);
	}

	public class SignatureOptions
	{
		[J("@type")] public required List<string> Type { get; set; }

		[J($"{Constants.W3IdSecurityNs}#signatureValue")]
		[JC(typeof(VC))]
		public string? Signature { get; set; }

		[J($"{Constants.PurlDcNs}/creator", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(ASObjectBaseConverter))]
		public ASObjectBase? Creator { get; set; }

		[J($"{Constants.W3IdSecurityNs}#nonce", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		public string? Nonce { get; set; }

		[J($"{Constants.W3IdSecurityNs}#domain", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		public string? Domain { get; set; }

		[J($"{Constants.PurlDcNs}/created", NullValueHandling = NullValueHandling.Ignore)]
		[JC(typeof(VC))]
		//FIXME: is this valid? it should output datetime in ISO format
		public DateTime? Created { get; set; }
	}
}