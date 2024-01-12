using Asp.Versioning;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG") ?? "configuration.ini",
                   false, true);
builder.Configuration
       .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG_OVERRIDES") ?? "configuration.overrides.ini",
                   true, true);

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddApiVersioning(options => {
	options.DefaultApiVersion               = new ApiVersion(1);
	options.ReportApiVersions               = true;
	options.UnsupportedApiVersionStatusCode = 501;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();
builder.Services.AddViteServices(options => {
	options.PackageDirectory     = "../Iceshrimp.Frontend";
	options.PackageManager       = "yarn";
	options.Server.AutoRun       = false; //TODO: Fix script generation on macOS
	options.Server.UseFullDevUrl = true;
});
//TODO: single line only if there's no \n in the log msg (otherwise stacktraces don't work)
builder.Services.AddLogging(logging => logging.AddSimpleConsole(options => { options.SingleLine = true; }));
builder.Services.AddDbContext<DatabaseContext>();

builder.Services.AddServices();
builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();
app.Logger.LogInformation("Initializing, please wait...");
app.Logger.LogInformation("Account domain: {AccountDomain}", Config.StartupConfig.Instance.AccountDomain);

app.UseSwagger();
app.UseSwaggerUI(options => { options.DocumentTitle = "Iceshrimp API documentation"; });
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback");
app.MapRazorPages();
app.MapFallbackToPage("/Shared/FrontendSPA");

if (app.Environment.IsDevelopment()) app.UseViteDevMiddleware();

app.Urls.Clear();
app.Urls.Add($"http://{Config.StartupConfig.Instance.WebDomain}:{Config.StartupConfig.Instance.ListenPort}");

app.Run();