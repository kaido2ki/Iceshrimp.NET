using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationExtensions {
	public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app) {
		return app.UseMiddleware<RequestBufferingMiddleware>()
		          .UseMiddleware<AuthorizedFetchMiddleware>();
	}

	public static Config.InstanceSection Initialize(this WebApplication app, string[] args) {
		var instanceConfig = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		                     throw new Exception("Failed to read Instance config section");

		app.Logger.LogInformation("Iceshrimp.NET v{version} ({domain})", instanceConfig.Version,
		                          instanceConfig.AccountDomain);

		var provider = app.Services.CreateScope();
		var context  = provider.ServiceProvider.GetService<DatabaseContext>();
		if (context == null) {
			app.Logger.LogCritical("Failed to initialize database context");
			Environment.Exit(1);
		}

		app.Logger.LogInformation("Verifying database connection...");
		if (!context.Database.CanConnect()) {
			app.Logger.LogCritical("Failed to connect to database");
			Environment.Exit(1);
		}

		if (args.Contains("--migrate") || args.Contains("--migrate-and-start")) {
			app.Logger.LogInformation("Running migrations...");
			context.Database.Migrate();
			if (args.Contains("--migrate")) Environment.Exit(0);
		}

		app.Logger.LogInformation("Initializing, please wait...");

		return instanceConfig;
	}
}