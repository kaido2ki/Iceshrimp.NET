using Asp.Versioning;
using Iceshrimp.Backend.Core.Database;
using Vite.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration.AddIniFile("configuration.ini", false, true);
builder.Configuration.AddIniFile("configuration.overrides.ini", true, true);

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddApiVersioning(options => {
	options.DefaultApiVersion               = new ApiVersion(1);
	options.ReportApiVersions               = true;
	options.UnsupportedApiVersionStatusCode = 501;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorPages();
//TODO: load built assets in production
builder.Services.AddViteServices(options => {
	options.PackageDirectory     = "../Iceshrimp.Frontend";
	options.PackageManager       = "yarn";
	options.Server.AutoRun       = false; //TODO: Fix script generation on macOS
	options.Server.UseFullDevUrl = true;
	options.Base                 = "frontend"; // relative to wwwroot
});
builder.Services.AddLogging(logging => logging.AddSimpleConsole(options => {
	options.SingleLine = true;
}));
builder.Services.AddDbContext<DatabaseContext>();

var app = builder.Build();
app.Logger.LogInformation("Initializing, please wait...");

app.UseSwagger();
app.UseSwaggerUI(options => { options.DocumentTitle = "Iceshrimp API documentation"; });
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToController("/api/{**slug}", "FallbackAction", "Fallback");
app.MapRazorPages();
app.MapFallbackToPage("/Shared/FrontendSPA");

if (app.Environment.IsDevelopment()) app.UseViteDevMiddleware();

app.Run();