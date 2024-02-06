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
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

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
		services.AddScoped<AuthenticationMiddleware>();

		//TODO: make this prettier
		services.AddScoped<Controllers.Mastodon.Renderers.UserRenderer>();
		services.AddScoped<Controllers.Mastodon.Renderers.NoteRenderer>();

		// Singleton = instantiated once across application lifetime
		services.AddSingleton<HttpClient>();
		services.AddSingleton<HttpRequestService>();
		services.AddSingleton<ActivityFetcherService>();
		services.AddSingleton<QueueService>();
		services.AddSingleton<ErrorHandlerMiddleware>();
		services.AddSingleton<RequestBufferingMiddleware>();
		services.AddSingleton<AuthorizationMiddleware>();
		services.AddSingleton<RequestVerificationMiddleware>();
		services.AddSingleton<RequestDurationMiddleware>();

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

	public static void AddRedis(this IServiceCollection services, IConfiguration configuration) {
		var instance = configuration.GetSection("Instance").Get<Config.InstanceSection>();
		var redis    = configuration.GetSection("Redis").Get<Config.RedisSection>();
		if (redis == null || instance == null)
			throw new Exception("Failed to initialize redis: Failed to load configuration");

		var redisOptions = new ConfigurationOptions {
			User            = redis.Username,
			Password        = redis.Password,
			DefaultDatabase = redis.Database,
			EndPoints = new EndPointCollection {
				{ redis.Host, redis.Port }
			}
		};

		services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));

		services.AddStackExchangeRedisCache(options => {
			options.InstanceName         = redis.Prefix ?? instance.WebDomain + ":cache:";
			options.ConfigurationOptions = redisOptions;
		});
	}

	public static void AddSwaggerGenWithOptions(this IServiceCollection services) {
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(options => {
			options.SwaggerDoc("v1", new OpenApiInfo { Title = "Iceshrimp.NET", Version = "1.0" });
			options.AddSecurityDefinition("user", new OpenApiSecurityScheme {
				Name   = "Authorization token",
				In     = ParameterLocation.Header,
				Type   = SecuritySchemeType.Http,
				Scheme = "bearer"
			});
			options.AddSecurityDefinition("admin", new OpenApiSecurityScheme {
				Name   = "Authorization token",
				In     = ParameterLocation.Header,
				Type   = SecuritySchemeType.Http,
				Scheme = "bearer"
			});
			options.AddSecurityDefinition("mastodon", new OpenApiSecurityScheme {
				Name   = "Authorization token",
				In     = ParameterLocation.Header,
				Type   = SecuritySchemeType.Http,
				Scheme = "bearer"
			});

			options.AddOperationFilters();
		});
	}

	public static void AddSlidingWindowRateLimiter(this IServiceCollection services) {
		//TODO: separate limiter for authenticated users, partitioned by user id
		//TODO: ipv6 /64 subnet buckets
		//TODO: rate limit status headers - maybe switch to https://github.com/stefanprodan/AspNetCoreRateLimit?
		//TODO: alternatively just write our own
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