using Iceshrimp.Backend.Core.Helpers;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace Iceshrimp.Backend.Controllers.Mastodon.Schemas;

public class OAuthAuthorizationServerResponse(string webDomain)
{
    [J("issuer")] public string Issuer => $"https://{webDomain}";

    [J("authorization_endpoint")] public string AuthorizationEndpoint => $"https://{webDomain}/oauth/authorize";

    [J("token_endpoint")] public string TokenEndpoint => $"https://{webDomain}/oauth/token";

    [J("app_registration_endpoint")] public string AppRegistrationEndpoint => $"https://{webDomain}/api/v1/apps";

    [J("scopes_supported")] public List<string> ScopesSupported => MastodonOauthHelpers.AllScopes;

    [J("response_types_supported")] public List<string> ResponseTypesSupported => ["code"];

    [J("response_modes_supported")] public List<string> ResponseModesSupported => ["query", "form_post"];

    [J("grant_types_supported")] public List<string> GrantTypesSupported => ["client_credentials"];

    [J("token_endpoint_auth_methods_supported")]
    public List<string> TokenEndpointAuthMethodsSupported => ["client_secret_post"];

    [J("service_documentation")] public string ServiceDocumentation => $"https://{webDomain}/openapi/";

    [J("ui_locales_supported")] public List<string> UiLocalesSupported => ["en"];

    [J("revocation_endpoint")] public string RevocationEndpoint => $"https://{webDomain}/oauth/revoke";

    [J("revocation_endpoint_auth_methods_supported")]
    public List<string> RevocationEndpointAuthMethodsSupported => ["client_secret_post"];
}
