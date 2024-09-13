using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Pleroma.Schemas;

public class FrontendConfigurationsResponse
{
	[J("pleroma_fe")] public PleromaFeConfiguration PleromaFe => new();
}

public class PleromaFeConfiguration
{
	[J("loginMethod")]     public string LoginMethod     => "token";
	[J("useStreamingApi")] public bool   UseStreamingApi => true;
}