using Asp.Versioning;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG") ?? "configuration.ini",
                   false, true);
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG_OVERRIDES") ?? "configuration.overrides.ini",
                   true, true);

builder.Services.AddControllers()
       .AddNewtonsoftJson() //TODO: remove once dotNetRdf switches to System.Text.Json
       .AddMultiFormatter();
builder.Services.AddApiVersioning(options => {
	options.DefaultApiVersion               = new ApiVersion(1);
	options.ReportApiVersions               = true;
	options.UnsupportedApiVersionStatusCode = 501;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
	options.SwaggerDoc("v1", new OpenApiInfo { Title = "Iceshrimp.NET", Version = "1.0" });
});
builder.Services.AddRazorPages();
builder.Services.AddViteServices(options => {
	options.PackageDirectory     = "../Iceshrimp.Frontend";
	options.PackageManager       = "yarn";
	options.Server.AutoRun       = false; //TODO: Fix script generation on macOS
	options.Server.UseFullDevUrl = true;
});
//TODO: single line only if there's no \n in the log msg (otherwise stacktraces don't work)
builder.Services.AddLogging(logging => logging.AddSimpleConsole(options => { options.SingleLine = true; }));
builder.Services.AddDatabaseContext(builder.Configuration); //TODO: maybe use a dbcontext factory?
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddSlidingWindowRateLimiter();

builder.Services.AddServices();
builder.Services.ConfigureServices(builder.Configuration);

var app    = builder.Build();
var config = app.Initialize(args);

// This determines the order of middleware execution in the request pipeline
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(options => {
	options.DocumentTitle = "Iceshrimp API documentation";
	options.SwaggerEndpoint("v1/swagger.json", "Iceshrimp.NET");
	options.InjectStylesheet("/swagger/styles.css");
	options.EnablePersistAuthorization();
	options.EnableTryItOutByDefault();
	options.DisplayRequestDuration();
	options.DefaultModelsExpandDepth(-1); // Hide "Schemas" section
});
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthorization();
app.UseCustomMiddleware();

app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback");
app.MapRazorPages();
app.MapFallbackToPage("/Shared/FrontendSPA");

if (app.Environment.IsDevelopment()) app.UseViteDevMiddleware();

app.Urls.Clear();
app.Urls.Add($"http://{config.WebDomain}:{config.ListenPort}");

app.Run();