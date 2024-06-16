using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Hubs;
using Iceshrimp.Backend.Hubs.Authentication;

StartupHelpers.ParseCliArguments(args);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddCustomConfiguration();

builder.Services.AddControllers()
       .AddNewtonsoftJson() //TODO: remove once dotNetRdf switches to System.Text.Json (or we switch to LinkedData.NET)
       .AddMultiFormatter()
       .AddModelBindingProviders()
       .AddValueProviderFactories()
       .AddApiBehaviorOptions();

builder.Services.AddSwaggerGenWithOptions();
builder.Services.AddLoggingWithOptions()
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddSlidingWindowRateLimiter();
builder.Services.AddCorsPolicies();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddAuthenticationServices();
builder.Services.AddSignalR().AddMessagePackProtocol();
builder.Services.AddResponseCompression();

#if DEBUG
if (builder.Environment.IsDevelopment())
	builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
else
	builder.Services.AddRazorPages();
#else
builder.Services.AddRazorPages();
#endif

builder.Services.AddServices();
builder.Services.ConfigureServices(builder.Configuration);
builder.WebHost.ConfigureKestrel(builder.Configuration);
builder.WebHost.UseStaticWebAssets();

var app    = builder.Build();
var config = await app.Initialize(args);

// This determines the order of middleware execution in the request pipeline
#if DEBUG
if (app.Environment.IsDevelopment())
	app.UseWebAssemblyDebugging();
else
	app.UseResponseCompression();
#else
app.UseResponseCompression();
#endif

app.UseRouting();
app.UseSwaggerWithOptions();
app.UseBlazorFrameworkFilesWithTransparentDecompression();
app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseCustomMiddleware();

app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback").WithOrder(int.MaxValue - 3);
app.MapHub<ExampleHub>("/hubs/example");
app.MapHub<StreamingHub>("/hubs/streaming");
app.MapRazorPages();
app.MapFrontendRoutes("/Shared/FrontendSPA");

app.Urls.Clear();
if (config.ListenSocket == null)
	app.Urls.Add($"{(args.Contains("--https") ? "https" : "http")}://{config.ListenHost}:{config.ListenPort}");

await app.StartAsync();
app.SetKestrelUnixSocketPermissions();
await app.WaitForShutdownAsync();