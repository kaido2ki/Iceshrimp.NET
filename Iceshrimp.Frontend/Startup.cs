using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Iceshrimp.Frontend;
using Iceshrimp.Frontend.Core.Services;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<ApiService>();
builder.Services.AddIntersectionObserver();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<StreamingService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddBlazoredLocalStorageAsSingleton();

await builder.Build().RunAsync(); 