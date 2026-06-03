namespace Iceshrimp.Shared.Schemas.Web;

public class WebPushSubscriptionResponse
{
    public required string Id       { get; set; }
    public required string Endpoint { get; set; }
    public required string VapidKey { get; set; }
}

public class WebPushSubscriptionRequest
{
    /// <summary>
    /// URL to client's push service
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Client's push service p256dh key
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// Client's push service auth secret
    /// </summary>
    public required string AuthSecret { get; set; }
}
