using Iceshrimp.Backend.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddCustomConfiguration();

builder.Services.AddControllers()
       .AddNewtonsoftJson() //TODO: remove once dotNetRdf switches to System.Text.Json (or we switch to LinkedData.NET)
       .AddMultiFormatter()
       .AddModelBindingProviders()
       .AddValueProviderFactories();

builder.Services.AddSwaggerGenWithOptions();
builder.Services.AddLogging(logging => logging.AddCustomConsoleFormatter());
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddSlidingWindowRateLimiter();
builder.Services.AddCorsPolicies();

if (builder.Environment.IsDevelopment())
	builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
else
	builder.Services.AddRazorPages();

builder.Services.AddServices();
builder.Services.ConfigureServices(builder.Configuration);
builder.WebHost.ConfigureKestrel(builder.Configuration);
builder.WebHost.UseStaticWebAssets();

var app    = builder.Build();
var config = await app.Initialize(args);

// This determines the order of middleware execution in the request pipeline
app.UseRouting();
app.UseSwaggerWithOptions();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseCors();
app.UseAuthorization();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
app.UseCustomMiddleware();
app.UseBlazorFrameworkFiles();

app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback");
app.MapRazorPages();
app.MapFallbackToPage("/Shared/FrontendSPA");

if (app.Environment.IsDevelopment())
	app.UseWebAssemblyDebugging();

app.Urls.Clear();
if (config.ListenSocket == null)
	app.Urls.Add($"http://{config.ListenHost}:{config.ListenPort}");

await app.StartAsync();
app.SetKestrelUnixSocketPermissions();
await app.WaitForShutdownAsync();