using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Migrations;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Backend.Core.Services.ImageProcessing;
using Iceshrimp.Utils.DependencyInjection;
using Iceshrimp.WebPush;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticAssets.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebApplicationExtensions
{
	extension(IApplicationBuilder app)
	{
		public IApplicationBuilder UseCustomMiddleware()
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
			          .UseOutputCache()
			          .UseMiddleware<BlazorSsrHandoffMiddleware>();
		}
	}

	extension(WebApplication app)
	{
		public IApplicationBuilder UseOpenApiWithOptions()
		{
			app.MapSwagger("/openapi/{documentName}.{extension:regex(^(json|ya?ml)$)}",
			               o =>
			               {
				               // Workaround for https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/3649
				               // Switch to 3_1 when fixed
				               o.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
			               })
			   .CacheOutput(p => p.Expire(TimeSpan.FromHours(12)))
			   .RequireCors("openapi");

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

			app.MapScalarApiReference("/scalar", options =>
			{
				options.WithTitle("Iceshrimp API documentation")
				       .AddDocument("iceshrimp", "Iceshrimp.NET")
				       .AddDocument("federation", "Federation")
				       .AddDocument("mastodon", "Mastodon")
				       .WithOpenApiRoutePattern("/openapi/{documentName}.json")
				       .HideModels()
				       .EnablePersistentAuthentication()
				       .DisableAgent()
				       .WithCustomCss("""
				                      .open-api-client-button, .darklight-reference > .flex > .text-sm { display: none !important; }
				                      .darklight-reference > .flex > button > div:nth-child(1) { height: 14px !important; }
				                      .darklight-reference { padding: 15px 14px !important; }
				                      """);
			});

			return app;
		}

		public async Task<Config.InstanceSection> InitializeAsync(string[] args)
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
				try
				{
					await db.Database.OpenConnectionAsync();
					await db.Database.CloseConnectionAsync();
				}
				catch (Exception e)
				{
					app.Logger.LogCritical("Failed to connect to database. Please make sure your configuration is correct.");
					app.Logger.LogError("Additional information: {e}", e);
					Environment.Exit(1);
				}

				app.Logger.LogCritical("Failed to connect to database. Please make sure your configuration is correct.");
				Environment.Exit(1);
			}

			var unknownMigrations = (await db.Database.GetAppliedMigrationsAsync())
			                        .Except(db.Database.GetMigrations())
			                        .ToList();

			if (unknownMigrations.Count > 0)
			{
				app.Logger.LogCritical("Database has {Count} unknown migrations applied, refusing to continue startup.", unknownMigrations.Count);
				app.Logger.LogCritical("If you tried to downgrade, make sure you know what you are doing and revert all migrations applied since the version you are downgrading to.");
				app.Logger.LogCritical("Unknown Migrations: {}", string.Join(", ", unknownMigrations));
				Environment.Exit(1);
			}

			// @formatter:off
			var pendingMigration = (await db.Database.GetPendingMigrationsAsync()).FirstOrDefault();
			if (args.Contains("--migrate-from-js"))
			{
				app.Logger.LogInformation("Initializing migration assistant...");
				var initialMigration = typeof(Initial).GetCustomAttribute<MigrationAttribute>()?.Id;
				if (pendingMigration != initialMigration || await db.IsDatabaseEmptyAsync())
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
				if (pendingMigration == initialMigration && !await db.IsDatabaseEmptyAsync())
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
				await provider.GetRequiredService<StorageMaintenanceService>()
				              .MigrateLocalFilesAsync(args.Contains("--purge"));
				Environment.Exit(0);
			}

			if (args.Contains("--fixup-media"))
			{
				await provider.GetRequiredService<StorageMaintenanceService>().FixupMediaAsync(args.Contains("--dry-run"));
				Environment.Exit(0);
			}

			if (args.Contains("--cleanup-storage"))
			{
				await provider.GetRequiredService<StorageMaintenanceService>()
				              .CleanupStorageAsync(args.Contains("--dry-run"));
				Environment.Exit(0);
			}

			string[] userMgmtCommands =
			[
				"--create-user", "--create-admin-user", "--reset-password", "--grant-admin", "--revoke-admin"
			];

			if (args.FirstOrDefault(userMgmtCommands.Contains) is { } cmd)
			{
				if (args is not [not null, var username, ..])
				{
					app.Logger.LogError("Invalid syntax. Usage: {cmd} <username> [--password <password>]", cmd);
					Environment.Exit(1);
					return null!;
				}

				if (cmd is "--create-user" or "--create-admin-user")
				{
					if (args is not [_, _, "--password", var password])
					{
						password = CryptographyHelpers.GenerateRandomString(16);
					}
					app.Logger.LogInformation("Creating user {username}...", username);
					var userSvc = provider.GetRequiredService<UserService>();
					await userSvc.CreateLocalUserAsync(username, password, null, force: true);

					if (args[0] is "--create-admin-user")
					{
						await db.Users
						        .Where(p => p.Username == username && p.Host == null)
						        .ExecuteUpdateAsync(p => p.SetProperty(i => i.IsAdmin, true));

						app.Logger.LogInformation("Successfully created admin user.");
					}
					else
					{
						app.Logger.LogInformation("Successfully created user.");
					}

					app.Logger.LogInformation("Username: {username}", username);
					app.Logger.LogInformation("Password: {password}", password);
					Environment.Exit(0);
				}

				if (cmd is "--reset-password")
				{
					var settings = await db.UserSettings
					                       .FirstOrDefaultAsync(p => p.User.UsernameLower == username.ToLowerInvariant());

					if (settings == null)
					{
						app.Logger.LogError("User {username} not found.", username);
						Environment.Exit(1);
					}

					app.Logger.LogInformation("Resetting password for user {username}...", username);

					var password = CryptographyHelpers.GenerateRandomString(16);
					settings.Password = AuthHelpers.HashPassword(password);
					await db.SaveChangesAsync();

					app.Logger.LogInformation("Password for user {username} was reset to: {password}", username, password);
					Environment.Exit(0);
				}

				if (cmd is "--grant-admin")
				{
					var user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == username.ToLowerInvariant() && p.Host == null);
					if (user == null)
					{
						app.Logger.LogError("User {username} not found.", username);
						Environment.Exit(1);
					}
					else
					{
						user.IsAdmin = true;
						await db.SaveChangesAsync();
						app.Logger.LogInformation("Granted admin privileges to user {username}.", username);
						Environment.Exit(0);
					}
				}

				if (cmd is "--revoke-admin")
				{
					var user = await db.Users.FirstOrDefaultAsync(p => p.UsernameLower == username.ToLowerInvariant() && p.Host == null);
					if (user == null)
					{
						app.Logger.LogError("User {username} not found.", username);
						Environment.Exit(1);
					}
					else
					{
						user.IsAdmin = false;
						await db.SaveChangesAsync();
						app.Logger.LogInformation("Revoked admin privileges of user {username}.", username);
						Environment.Exit(0);
					}
				}
			}

			var storageConfig = app.Configuration.GetSection("Storage").Get<Config.StorageSection>() ??
			                    throw new Exception("Failed to read Storage config section");

			if (storageConfig.Provider == Enums.FileStorage.Local)
			{
				if (string.IsNullOrWhiteSpace(storageConfig.Local?.Path) || !Directory.Exists(storageConfig.Local.Path))
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

			var tempPath = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
			try
			{
				await using var stream = File.Create(Path.Combine(tempPath, ".iceshrimp-test"), 1, FileOptions.DeleteOnClose);
			}
			catch
			{
				app.Logger.LogCritical("Temporary directory {dir} is not writable. Please adjust permissions or set the ASPNETCORE_TEMP environment variable to a writable directory.",
				                       tempPath);
				Environment.Exit(1);
			}

			app.Logger.LogInformation("Initializing VAPID keys...");
			var meta = provider.GetRequiredService<MetaService>();
			await meta.EnsureSetAsync([MetaEntity.VapidPublicKey, MetaEntity.VapidPrivateKey], () =>
			{
				var keypair = VapidHelper.GenerateVapidKeys();
				return [keypair.PublicKey, keypair.PrivateKey];
			});

			app.Logger.LogInformation("Warming up meta cache...");
			await meta.WarmupCacheAsync();

			// Initialize image processing
			provider.GetRequiredService<ImageProcessor>();

			return instanceConfig;
		}

		public void SetKestrelUnixSocketPermissions()
		{
			var config = app.Configuration.GetSection("Instance").Get<Config.InstanceSection>()
			             ?? throw new Exception("Failed to read instance config");
			if (config.ListenSocket == null) return;
			using var scope = app.Services.CreateScope();
			var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
			                  .CreateLogger("Microsoft.Hosting.Lifetime");

			if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsFreeBSD())
				throw new Exception("Can't set unix socket permissions on a non-UNIX system");

			int perms;
			try
			{
				perms = Convert.ToInt32(config.ListenSocketPerms, 8);
			}
			catch
			{
				logger.LogError("Failed to set Kestrel unix socket permissions to {SocketPerms}: failed to parse octal digits",
				                config.ListenSocketPerms);
				Environment.Exit(1);
				return;
			}

			var exitCode = chmod(config.ListenSocket, perms);
			if (exitCode < 0)
			{
				logger.LogError("Failed to set Kestrel unix socket permissions to {SocketPerms}, return code: {ExitCode}",
				                config.ListenSocketPerms, exitCode);
			}
			else
			{
				logger.LogInformation("Kestrel unix socket permissions were set to {SocketPerms}",
				                      config.ListenSocketPerms);
			}

			return;

			[DllImport("libc")]
			static extern int chmod(string pathname, int mode);
		}
	}

	extension(IEndpointRouteBuilder endpoints)
	{
		public void MapFrontendRoutes<TComponent>() where TComponent : IComponent
		{
			endpoints = endpoints.WithStaticAssets();
			endpoints.MapFallbackToRazorComponent<TComponent>("/@{user}").CacheOutput().WithOrder(int.MaxValue - 1);
			endpoints.MapFallbackToRazorComponent<TComponent>().CacheOutput().WithOrder(int.MaxValue - 2);
			endpoints.MapFallbackToRazorComponent<TComponent>("/@{user}@{host}").CacheOutput().WithOrder(int.MaxValue);
		}

		public IEndpointConventionBuilder MapFallbackToRazorComponent<TComponent>(
			[StringSyntax("Route")] string? route = null
		) where TComponent : IComponent
		{
			return route == null
				? endpoints.MapFallback(() => new RazorComponentResult<TComponent>())
				: endpoints.MapFallback(route, () => new RazorComponentResult<TComponent>());
		}

		// Temporary fix until https://github.com/dotnet/aspnetcore/issues/63398 is merged upstream
        public RouteGroupBuilder WithStaticAssets(string? staticAssetsManifestPath = null)
        {
            endpoints.MapStaticAssets(staticAssetsManifestPath);

            var resources = new List<ResourceAsset>();

            foreach (var descriptor in StaticAssetsEndpointDataSourceHelper.ResolveStaticAssetDescriptors(endpoints, null))
            {
                string? label     = null;
                string? integrity = null;

                if (descriptor.Selectors.Count != 0)
                {
                    continue;
                }

                var foundProperties = 0;

                foreach (var property in descriptor.Properties)
                {
                    if (property.Name.Equals("label", StringComparison.OrdinalIgnoreCase))
                    {
                        label = property.Value;
                        foundProperties++;
                    }
                    else if (property.Name.Equals("integrity", StringComparison.OrdinalIgnoreCase))
                    {
                        integrity = property.Value;
                        foundProperties++;
                    }
                }

                if (label != null || integrity != null)
                {
                    var properties = new ResourceAssetProperty[foundProperties];
                    var index      = 0;
                    if (label != null)
                    {
                        properties[index++] = new("label", label);
                    }

                    if (integrity != null)
                    {
                        properties[index] = new("integrity", integrity);
                    }

                    resources.Add(new ResourceAsset(descriptor.Route, properties));
                }
                else
                {
                    resources.Add(new ResourceAsset(descriptor.Route));
                }
            }

            resources.Sort((a, b) => string.Compare(a.Url, b.Url, StringComparison.Ordinal));

            return endpoints.MapGroup(string.Empty).WithMetadata(new ResourceAssetCollection(new ResourceAssetCollection(resources)));
        }
	}
}