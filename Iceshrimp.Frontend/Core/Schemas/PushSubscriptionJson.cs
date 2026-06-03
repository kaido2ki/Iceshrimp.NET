using System.Text.Json.Serialization;

namespace Iceshrimp.Frontend.Core.Schemas;

public sealed class PushSubscriptionJson
{
    [JsonPropertyName("endpoint")]
    [JsonRequired]
    public string Endpoint { get; set; } = null!;

    [JsonPropertyName("expirationTime")]
    [JsonRequired]
    public string? ExpirationTime { get; set; }

    [JsonPropertyName("keys")]
    [JsonRequired]
    public PushKeys Keys { get; set; } = null!;

    public sealed class PushKeys
    {
        [JsonPropertyName("auth")]
        [JsonRequired]
        public string Auth { get; set; } = null!;

        [JsonPropertyName("p256dh")]
        [JsonRequired]
        public string P256Dh { get; set; } = null!;
    }
}
