using System.Runtime.InteropServices;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationExtensions
{
	public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
	{
		// Caution: make sure these are in the correct order
		return app.UseMiddleware<RequestDurationMiddleware>()
		          .UseMiddleware<ErrorHandlerMiddleware>()
		          .UseMiddleware<RequestVerificationMiddleware>()
		          .UseMiddleware<RequestBufferingMiddleware>()
		          .UseMiddleware<AuthenticationMiddleware>()
		          .UseMiddleware<AuthorizationMiddleware>()
		          .UseMiddleware<AuthorizedFetchMiddleware>()
		          .UseMiddleware<InboxValidationMiddleware>();
	}

	public static IApplicationBuilder UseSwaggerWithOptions(this WebApplication app)
	{
		app.UseSwagger();
		app.UseSwaggerUI(options =>
		{
			options.DocumentTitle = "Iceshrimp API documentation";
			options.SwaggerEndpoint("iceshrimp/swagger.json", "Iceshrimp.NET");
			options.SwaggerEndpoint("federation/swagger.json", "Federation");
			options.SwaggerEndpoint("mastodon/swagger.json", "Mastodon");
			options.InjectStylesheet("/swagger/styles.css");
			options.EnablePersistAuthorization();
			options.EnableTryItOutByDefault();
			options.DisplayRequestDuration();
			options.DefaultModelsExpandDepth(-1);                            // Hide "Schemas" section
			options.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha"); // Sort tags alphabetically
		});
		return app;
	}

	public static async Task<Config.InstanceSection> Initialize(this WebApplication app, string[] args)
	{
		var instanceConfig = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		                     throw new Exception("Failed to read Instance config section");

		app.Logger.LogInformation("Iceshrimp.NET v{version} ({domain})", instanceConfig.Version,
		                          instanceConfig.AccountDomain);
		try
		{
			app.Logger.LogInformation("Validating configuration...");
			app.Services.CreateScope().ServiceProvider.GetRequiredService<IStartupValidator>().Validate();
		}
		catch (OptionsValidationException e)
		{
			app.Logger.LogCritical("Failed to validate configuration: {error}", e.Message);
			Environment.Exit(1);
		}

		if (app.Environment.IsDevelopment())
		{
			app.Logger.LogWarning("The hosting environment is set to Development.");
			app.Logger.LogWarning("This application will not validate the Host header for incoming requests.");
			app.Logger.LogWarning("If this is not a local development instance, please set the environment to Production.");
		}

		var provider = app.Services.CreateScope().ServiceProvider;
		var context  = provider.GetService<DatabaseContext>();
		if (context == null)
		{
			app.Logger.LogCritical("Failed to initialize database context");
			Environment.Exit(1);
		}

		app.Logger.LogInformation("Verifying database connection...");
		if (!await context.Database.CanConnectAsync())
		{
			app.Logger.LogCritical("Failed to connect to database");
			Environment.Exit(1);
		}

		if (args.Contains("--migrate") || args.Contains("--migrate-and-start"))
		{
			app.Logger.LogInformation("Running migrations...");
			context.Database.SetCommandTimeout(0);
			await context.Database.MigrateAsync();
			context.Database.SetCommandTimeout(30);
			if (args.Contains("--migrate")) Environment.Exit(0);
		}
		else if ((await context.Database.GetPendingMigrationsAsync()).Any())
		{
			app.Logger.LogCritical("Database has pending migrations, please restart with --migrate or --migrate-and-start");
			Environment.Exit(1);
		}

		if (args.Contains("--recompute-counters"))
		{
			app.Logger.LogInformation("Recomputing note, user & instance counters, this will take a while...");
			var maintenanceSvc = provider.GetRequiredService<DatabaseMaintenanceService>();
			await maintenanceSvc.RecomputeNoteCountersAsync();
			await maintenanceSvc.RecomputeUserCountersAsync();
			await maintenanceSvc.RecomputeInstanceCountersAsync();
		}

		app.Logger.LogInformation("Verifying redis connection...");
		var cache = provider.GetService<IDistributedCache>();
		if (cache == null)
		{
			app.Logger.LogCritical("Failed to initialize redis cache");
			Environment.Exit(1);
		}

		try
		{
			await cache.GetAsync("test");
		}
		catch
		{
			app.Logger.LogCritical("Failed to connect to redis");
			Environment.Exit(1);
		}

		var storageConfig = app.Configuration.GetSection("Storage").Get<Config.StorageSection>() ??
		                    throw new Exception("Failed to read Storage config section");

		if (storageConfig.Mode == Enums.FileStorage.Local)
		{
			if (string.IsNullOrWhiteSpace(storageConfig.Local?.Path) ||
			    !Directory.Exists(storageConfig.Local.Path))
			{
				app.Logger.LogCritical("Local storage path does not exist");
				Environment.Exit(1);
			}
			else
			{
				try
				{
					var path = Path.Combine(storageConfig.Local.Path, Path.GetRandomFileName());

					await using var fs = File.Create(path, 1, FileOptions.DeleteOnClose);
				}
				catch
				{
					app.Logger.LogCritical("Local storage path is not accessible or not writable");
					Environment.Exit(1);
				}
			}
		}
		else if (storageConfig.Mode == Enums.FileStorage.ObjectStorage)
		{
			app.Logger.LogInformation("Verifying object storage configuration...");
			var svc = provider.GetRequiredService<ObjectStorageService>();
			try
			{
				await svc.VerifyCredentialsAsync();
			}
			catch (Exception e)
			{
				app.Logger.LogCritical("Failed to initialize object storage: {message}", e.Message);
				Environment.Exit(1);
			}
		}

		app.Logger.LogInformation("Initializing application, please wait...");

		return instanceConfig;
	}

	public static void SetKestrelUnixSocketPermissions(this WebApplication app)
	{
		var config = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		             throw new Exception("Failed to read instance config");
		if (config.ListenSocket == null) return;
		var logger = app.Services.CreateScope()
		                .ServiceProvider.GetRequiredService<ILoggerFactory>()
		                .CreateLogger("Microsoft.Hosting.Lifetime");

		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
			throw new Exception("Can't set unix socket permissions on a non-UNIX system");

		var perms    = "660";
		var exitCode = chmod(config.ListenSocket, Convert.ToInt32(perms, 8));

		if (exitCode < 0)
			logger.LogError("Failed to set Kestrel unix socket permissions to {SocketPerms}, return code: {ExitCode}",
			                perms, exitCode);
		else
			logger.LogInformation("Kestrel unix socket permissions were set to {SocketPerms}", perms);

		return;

		[DllImport("libc")]
		static extern int chmod(string pathname, int mode);
	}
}