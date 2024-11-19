using System.Diagnostics;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Pages.Shared;
using Iceshrimp.Backend.SignalR;
using Iceshrimp.Backend.SignalR.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

var options = StartupHelpers.ParseCliArguments(args);
var builder = WebApplication.CreateBuilder(options);

builder.Configuration.Sources.Clear();
builder.Configuration.AddCustomConfiguration();

await PluginLoader.LoadPluginsAsync();

builder.Services
       .AddControllersWithOptions()
       .AddNewtonsoftJson() //TODO: remove once dotNetRdf switches to System.Text.Json (or we switch to LinkedData.NET)
       .ConfigureNewtonsoftJson()
       .AddMultiFormatter()
       .AddModelBindingProviders()
       .AddValueProviderFactories()
       .AddApiBehaviorOptions()
       .AddPlugins(PluginLoader.Assemblies);

builder.Services.AddOpenApiWithOptions();
builder.Services.AddOutputCacheWithOptions();
builder.Services.AddLogging(logging => logging.AddCustomConsoleFormatter());
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddSlidingWindowRateLimiter();
builder.Services.AddCorsPolicies();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddAuthenticationServices();
builder.Services.AddSignalR().AddMessagePackProtocol();
builder.Services.AddResponseCompression();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents();
builder.Services.AddAntiforgery(o => o.Cookie.Name = "CSRF-Token");
builder.Services.AddMiddleware();
builder.Services.AddServices(builder.Configuration);
builder.Services.ConfigureServices(builder.Configuration);

builder.WebHost.ConfigureKestrel(builder.Configuration);
builder.WebHost.UseStaticWebAssets();

PluginLoader.RunBuilderHooks(builder);

var app    = builder.Build();
var config = await app.InitializeAsync(args);

// This determines the order of middleware execution in the request pipeline
#if DEBUG
if (app.Environment.IsDevelopment())
	app.UseWebAssemblyDebugging();
else
	app.UseResponseCompression();
#else
app.UseResponseCompression();
#endif

app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedProto });
app.UseRouting();
app.UseOpenApiWithOptions();
app.UseCors();
app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseCustomMiddleware();
app.UseAntiforgery();

app.MapStaticAssetsWithTransparentDecompression();
app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback").WithOrder(int.MaxValue - 3);
app.MapHub<StreamingHub>("/hubs/streaming");
app.MapRazorPages().WithStaticAssets();
app.MapRazorComponents<RootComponent>();
app.MapFrontendRoutes("/Shared/FrontendSPA");

PluginLoader.RunAppHooks(app);
PluginLoader.PrintPluginInformation(app);

// If running under IIS, this collection is read only
if (!app.Urls.IsReadOnly)
{
	app.Urls.Clear();
	if (config.ListenSocket == null)
		app.Urls.Add($"{(args.Contains("--https") ? "https" : "http")}://{config.ListenHost}:{config.ListenPort}");
}

var elapsed = (DateTime.Now - Process.GetCurrentProcess().StartTime).GetTotalMilliseconds();
app.Logger.LogInformation("Startup complete after {ms} ms.", elapsed);
await app.StartAsync();
app.SetKestrelUnixSocketPermissions();
await app.WaitForShutdownAsync();
