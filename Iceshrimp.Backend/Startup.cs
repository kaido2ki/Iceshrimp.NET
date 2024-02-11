using Asp.Versioning;
using Iceshrimp.Backend.Core.Extensions;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG") ?? "configuration.ini",
                   false, true);
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG_OVERRIDES") ?? "configuration.overrides.ini",
                   true, true);

builder.Services.AddControllers(options => { options.ModelBinderProviders.AddHybridBindingProvider(); })
       .AddNewtonsoftJson() //TODO: remove once dotNetRdf switches to System.Text.Json (or we switch to LinkedData.NET)
       .AddMultiFormatter();
builder.Services.AddApiVersioning(options => {
	options.DefaultApiVersion               = new ApiVersion(1);
	options.ReportApiVersions               = true;
	options.UnsupportedApiVersionStatusCode = 501;
});
builder.Services.AddSwaggerGenWithOptions();
builder.Services.AddRazorPages();
builder.Services.AddViteServices(options => {
	options.PackageDirectory     = "../Iceshrimp.Frontend";
	options.PackageManager       = "yarn";
	options.Server.AutoRun       = false; //TODO: Fix script generation on macOS
	options.Server.UseFullDevUrl = true;
});
builder.Services.AddLogging(logging => logging.AddCustomConsoleFormatter());
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSlidingWindowRateLimiter();
builder.Services.AddCorsPolicies();

builder.Services.AddServices();
builder.Services.ConfigureServices(builder.Configuration);

var app    = builder.Build();
var config = await app.Initialize(args);

// This determines the order of middleware execution in the request pipeline
app.UseRouting();
app.UseSwaggerWithOptions();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseCors();
app.UseAuthorization();
app.UseCustomMiddleware();

app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback");
app.MapRazorPages();
app.MapFallbackToPage("/Shared/FrontendSPA");

if (app.Environment.IsDevelopment()) app.UseViteDevMiddleware();

app.Urls.Clear();
app.Urls.Add($"http://{config.ListenHost}:{config.ListenPort}");

app.Run();