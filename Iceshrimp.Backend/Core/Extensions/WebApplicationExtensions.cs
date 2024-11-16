using System.Runtime.InteropServices;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Migrations;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Backend.Core.Services.ImageProcessing;
using Iceshrimp.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

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
		          .UseRateLimiter()
		          .UseMiddleware<AuthorizationMiddleware>()
		          .UseMiddleware<FederationSemaphoreMiddleware>()
		          .UseMiddleware<AuthorizedFetchMiddleware>()
		          .UseMiddleware<InboxValidationMiddleware>()
		          .UseMiddleware<BlazorSsrHandoffMiddleware>();
	}

	public static IApplicationBuilder UseOpenApiWithOptions(this WebApplication app)
	{
		app.MapSwagger("/openapi/{documentName}.{extension:regex(^(json|ya?ml)$)}")
		   .CacheOutput(p => p.Expire(TimeSpan.FromHours(12)));

		app.UseSwaggerUI(options =>
		{
			options.DocumentTitle = "Iceshrimp API documentation";
			options.SwaggerEndpoint("/openapi/iceshrimp.json", "Iceshrimp.NET");
			options.SwaggerEndpoint("/openapi/federation.json", "Federation");
			options.SwaggerEndpoint("/openapi/mastodon.json", "Mastodon");
			options.InjectStylesheet("/css/swagger.css");
			options.EnablePersistAuthorization();
			options.EnableTryItOutByDefault();
			options.DisplayRequestDuration();
			options.DefaultModelsExpandDepth(-1);                            // Hide "Schemas" section
			options.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha"); // Sort tags alphabetically
		});

		app.MapScalarApiReference(options =>
		{
			options.Title               = "Iceshrimp API documentation";
			options.OpenApiRoutePattern = "/openapi/{documentName}.json";
			options.EndpointPathPrefix  = "/scalar/{documentName}";
			options.HideModels          = true;

			options.CustomCss = """
			                    .open-api-client-button, .darklight-reference-promo { display: none !important; }
			                    .darklight { height: 14px !important; }
			                    .darklight-reference { padding: 14px !important; }
			                    """;
		});

		return app;
	}

	public static void MapFrontendRoutes(this WebApplication app, string page)
	{
		app.MapFallbackToPage(page).WithOrder(int.MaxValue - 2);
		app.MapFallbackToPage("/@{user}", page).WithOrder(int.MaxValue - 1);
		app.MapFallbackToPage("/@{user}@{host}", page);
	}

	public static async Task<Config.InstanceSection> Initialize(this WebApplication app, string[] args)
	{
		var instanceConfig = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		                     throw new Exception("Failed to read Instance config section");

		app.Logger.LogInformation("Iceshrimp.NET v{version}, codename \"{codename}\" ({domain})",
		                          instanceConfig.Version, instanceConfig.Codename, instanceConfig.AccountDomain);

		await using var scope    = app.Services.CreateAsyncScope();
		var             provider = scope.ServiceProvider;

		var config = (ConfigurationManager)app.Configuration;
		var files  = config.Sources.OfType<IniConfigurationSource>().Select(p => p.Path);
		app.Logger.LogDebug("Loaded configuration files: \n* {files}", string.Join("\n* ", files));

		try
		{
			app.Logger.LogInformation("Validating configuration...");
			provider.GetRequiredService<IStartupValidator>().Validate();
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

		await using var db = provider.GetService<DatabaseContext>();
		if (db == null)
		{
			app.Logger.LogCritical("Failed to initialize database context");
			Environment.Exit(1);
		}

		app.Logger.LogInformation("Verifying database connection...");
		if (!await db.Database.CanConnectAsync())
		{
			app.Logger.LogCritical("Failed to connect to database. Please make sure your configuration is correct.");
			Environment.Exit(1);
		}

		// @formatter:off
		var pendingMigration = (await db.Database.GetPendingMigrationsAsync()).FirstOrDefault();
		if (args.Contains("--migrate-from-js"))
		{
			app.Logger.LogInformation("Initializing migration assistant...");
			var initialMigration = typeof(Initial).GetCustomAttribute<MigrationAttribute>()?.Id;
			if (pendingMigration != initialMigration || await db.IsDatabaseEmpty())
			{
				app.Logger.LogCritical("Database does not appear to be an iceshrimp-js database.");
				Environment.Exit(1);
			}
			else if (!args.Contains("--i-reverted-any-extra-migrations") ||
			         !args.Contains("--i-made-a-database-backup") ||
			         !args.Contains("--i-understand-that-this-is-a-one-way-operation"))
			{
				app.Logger.LogCritical("Missing confirmation argument(s), please follow the instructions on https://iceshrimp.net/help/migrate exactly.");
				Environment.Exit(1);
			}
			else
			{
				app.Logger.LogInformation("Applying initial migration...");
				try
				{
					await db.Database.ExecuteSqlAsync(new MigrationAssistant().InitialMigration);
				}
				catch (Exception e)
				{
					app.Logger.LogCritical("Failed to apply initial migration: {error}", e);
					app.Logger.LogCritical("Manual intervention required, please follow the instructions on https://iceshrimp.net/help/migrate for more information.");
					Environment.Exit(1);
				}

				app.Logger.LogInformation("Successfully applied the initial migration.");
				app.Logger.LogInformation("Please follow the instructions on https://iceshrimp.net/help/migrate to validate the database schema.");
				Environment.Exit(0);
			}
		}
		// @formatter:on

		if (pendingMigration != null)
		{
			var initialMigration = typeof(Initial).GetCustomAttribute<MigrationAttribute>()?.Id;
			if (pendingMigration == initialMigration && !await db.IsDatabaseEmpty())
			{
				app.Logger.LogCritical("Initial migration is pending but database is not empty.");
				app.Logger.LogCritical("If you are trying to migrate from iceshrimp-js, please follow the instructions on https://iceshrimp.net/help/migrate.");
				Environment.Exit(1);
			}

			if (args.Contains("--migrate") || args.Contains("--migrate-and-start"))
			{
				app.Logger.LogInformation("Running migrations...");
				db.Database.SetCommandTimeout(0);
				await db.Database.MigrateAsync();
				db.Database.SetCommandTimeout(30);
				if (args.Contains("--migrate")) Environment.Exit(0);
			}
			else
			{
				app.Logger.LogCritical("Database has pending migrations, please restart with --migrate or --migrate-and-start");
				Environment.Exit(1);
			}
		}
		else if (args.Contains("--migrate") || args.Contains("--migrate-and-start"))
		{
			app.Logger.LogInformation("No migrations are pending.");
			if (args.Contains("--migrate")) Environment.Exit(0);
		}

		if (args.Contains("--recompute-counters"))
		{
			app.Logger.LogInformation("Recomputing note, user & instance counters, this will take a while...");
			var maintenanceSvc = provider.GetRequiredService<DatabaseMaintenanceService>();
			await maintenanceSvc.RecomputeNoteCountersAsync();
			await maintenanceSvc.RecomputeUserCountersAsync();
			await maintenanceSvc.RecomputeInstanceCountersAsync();
			Environment.Exit(0);
		}

		if (args.Contains("--migrate-storage"))
		{
			app.Logger.LogInformation("Migrating files to object storage, this will take a while...");
			db.Database.SetCommandTimeout(0);
			await provider.GetRequiredService<StorageMaintenanceService>().MigrateLocalFiles(args.Contains("--purge"));
			Environment.Exit(0);
		}

		if (args.Contains("--fixup-media"))
		{
			await provider.GetRequiredService<StorageMaintenanceService>().FixupMedia(args.Contains("--dry-run"));
			Environment.Exit(0);
		}

		if (args.Contains("--cleanup-storage"))
		{
			await provider.GetRequiredService<StorageMaintenanceService>().CleanupStorage(args.Contains("--dry-run"));
			Environment.Exit(0);
		}

		var storageConfig = app.Configuration.GetSection("Storage").Get<Config.StorageSection>() ??
		                    throw new Exception("Failed to read Storage config section");

		if (storageConfig.Provider == Enums.FileStorage.Local)
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
		else if (storageConfig.Provider == Enums.FileStorage.ObjectStorage)
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

		app.Logger.LogInformation("Initializing VAPID keys...");
		var meta = provider.GetRequiredService<MetaService>();
		await meta.EnsureSet([MetaEntity.VapidPublicKey, MetaEntity.VapidPrivateKey], () =>
		{
			var keypair = VapidHelper.GenerateVapidKeys();
			return [keypair.PublicKey, keypair.PrivateKey];
		});

		app.Logger.LogInformation("Warming up meta cache...");
		await meta.WarmupCache();

		// Initialize image processing
		provider.GetRequiredService<ImageProcessor>();

		return instanceConfig;
	}

	public static void SetKestrelUnixSocketPermissions(this WebApplication app)
	{
		var config = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		             throw new Exception("Failed to read instance config");
		if (config.ListenSocket == null) return;
		using var scope = app.Services.CreateScope();
		var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
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