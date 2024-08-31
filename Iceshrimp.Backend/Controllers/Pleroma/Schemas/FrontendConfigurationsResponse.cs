using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas;

public class FrontendConfigurationsResponse()
{
    [J("pleroma_fe")] public PleromaFeConfiguration PleromaFe => new();
}


// TODO: STUB
public class PleromaFeConfiguration()
{
    // Use Oauth
    [J("loginMethod")] public string loginMethod => "token";
}
