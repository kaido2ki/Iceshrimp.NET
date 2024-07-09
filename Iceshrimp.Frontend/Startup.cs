using System.Globalization;
using Blazored.LocalStorage;
using Iceshrimp.Frontend;
using Iceshrimp.Frontend.Core.Miscellaneous;
using Iceshrimp.Frontend.Core.Services;
using Ljbc1994.Blazor.IntersectionObserver;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization();
builder.Services.AddSingleton<ApiClient>();
builder.Services.AddSingleton<ApiService>();
builder.Services.AddIntersectionObserver();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<StreamingService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddSingleton<ComposeService>();
builder.Services.AddSingleton<StateService>();
builder.Services.AddSingleton<EmojiService>();
builder.Services.AddSingleton<VersionService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddBlazoredLocalStorageAsSingleton();

// Culture information (locale) has to be set before run.
var host    = builder.Build();
var helper  = new LocaleHelper(host.Services.GetRequiredService<ISyncLocalStorageService>());
var culture = helper.LoadCulture();
CultureInfo.DefaultThreadCurrentCulture   = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;
await host.RunAsync();