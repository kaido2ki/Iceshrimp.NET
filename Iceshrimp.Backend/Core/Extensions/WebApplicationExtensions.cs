using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationExtensions {
	public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app) {
		// Caution: make sure these are in the correct order
		return app.UseMiddleware<ErrorHandlerMiddleware>()
		          .UseMiddleware<RequestBufferingMiddleware>()
		          .UseMiddleware<AuthenticationMiddleware>()
		          .UseMiddleware<AuthorizationMiddleware>()
		          .UseMiddleware<OauthAuthenticationMiddleware>()
		          .UseMiddleware<OauthAuthorizationMiddleware>()
		          .UseMiddleware<AuthorizedFetchMiddleware>();
	}

	public static IApplicationBuilder UseSwaggerWithOptions(this WebApplication app) {
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
		return app;
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

		app.Logger.LogInformation("Verifying redis connection...");
		var cache = provider.ServiceProvider.GetService<IDistributedCache>();
		if (cache == null) {
			app.Logger.LogCritical("Failed to initialize redis cache");
			Environment.Exit(1);
		}

		try {
			cache.Get("test");
		}
		catch {
			app.Logger.LogCritical("Failed to connect to redis");
			Environment.Exit(1);
		}

		app.Logger.LogInformation("Initializing application, please wait...");

		return instanceConfig;
	}
}