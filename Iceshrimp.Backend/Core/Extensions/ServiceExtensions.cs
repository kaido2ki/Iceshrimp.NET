using System.Threading.RateLimiting;
using Iceshrimp.Backend.Controllers.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.RateLimiting;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ServiceExtensions {
	public static void AddServices(this IServiceCollection services) {
		// Transient = instantiated per request and class
		//services.AddTransient<T>();

		// Scoped = instantiated per request
		services.AddScoped<ActivityRenderer>();
		services.AddScoped<UserRenderer>();
		services.AddScoped<NoteRenderer>();
		services.AddScoped<UserResolver>();
		services.AddScoped<UserService>();
		services.AddScoped<NoteService>();
		services.AddScoped<ActivityDeliverService>();
		services.AddScoped<ActivityHandlerService>();
		services.AddScoped<WebFingerService>();
		services.AddScoped<AuthorizedFetchMiddleware>();

		// Singleton = instantiated once across application lifetime
		services.AddSingleton<HttpClient>();
		services.AddSingleton<HttpRequestService>();
		services.AddSingleton<ActivityFetcherService>();
		services.AddSingleton<QueueService>();
		services.AddSingleton<ErrorHandlerMiddleware>();
		services.AddSingleton<RequestBufferingMiddleware>();

		// Hosted services = long running background tasks
		// Note: These need to be added as a singleton as well to ensure data consistency
		services.AddHostedService<QueueService>(provider => provider.GetRequiredService<QueueService>());
	}

	public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration) {
		//TODO: fail if config doesn't parse correctly / required things are missing
		services.Configure<Config>(configuration);
		services.Configure<Config.InstanceSection>(configuration.GetSection("Instance"));
		services.Configure<Config.SecuritySection>(configuration.GetSection("Security"));
		services.Configure<Config.DatabaseSection>(configuration.GetSection("Database"));
	}

	public static void AddDatabaseContext(this IServiceCollection services, IConfiguration configuration) {
		var config     = configuration.GetSection("Database").Get<Config.DatabaseSection>();
		var dataSource = DatabaseContext.GetDataSource(config);
		services.AddDbContext<DatabaseContext>(options => { DatabaseContext.Configure(options, dataSource); });
		services.AddDataProtection()
		        .PersistKeysToDbContext<DatabaseContext>()
		        .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration {
			        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
			        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
		        });
	}

	public static void AddSlidingWindowRateLimiter(this IServiceCollection services) {
		//TODO: separate limiter for authenticated users, partitioned by user id
		//TODO: ipv6 /64 subnet buckets
		//TODO: rate limit status headers - maybe switch to https://github.com/stefanprodan/AspNetCoreRateLimit?
		services.AddRateLimiter(options => {
			options.AddSlidingWindowLimiter("sliding", limiterOptions => {
				limiterOptions.PermitLimit          = 500;
				limiterOptions.SegmentsPerWindow    = 60;
				limiterOptions.Window               = TimeSpan.FromSeconds(60);
				limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
				limiterOptions.QueueLimit           = 0;
			});
			options.AddSlidingWindowLimiter("strict", limiterOptions => {
				limiterOptions.PermitLimit          = 10;
				limiterOptions.SegmentsPerWindow    = 60;
				limiterOptions.Window               = TimeSpan.FromSeconds(60);
				limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
				limiterOptions.QueueLimit           = 0;
			});
			options.OnRejected = async (context, token) => {
				context.HttpContext.Response.StatusCode  = 429;
				context.HttpContext.Response.ContentType = "application/json";
				var res = new ErrorResponse {
					Error      = "Too Many Requests",
					StatusCode = 429,
					RequestId  = context.HttpContext.TraceIdentifier
				};
				await context.HttpContext.Response.WriteAsJsonAsync(res, token);
			};
		});
	}
}