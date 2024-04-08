using System.Security.Claims;

namespace Iceshrimp.Frontend.Core.Services;
using Microsoft.AspNetCore.Components.Authorization;

internal class CustomAuthStateProvider (SessionService sessionService) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (sessionService.Current != null) { 
          var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, sessionService.Current.Username), }, "Custom Authentication");
          var user = new ClaimsPrincipal(identity);
          return Task.FromResult(new AuthenticationState(user));
        }
        else
        {
            var identity = new ClaimsIdentity();
            var user     = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}