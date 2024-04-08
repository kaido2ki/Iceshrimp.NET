using System.Threading.RateLimiting;
using Iceshrimp.Backend.Controllers.Federation;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Shared.Schemas;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Helpers.LibMfm.Conversion;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.Backend.Hubs.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using AuthenticationMiddleware = Iceshrimp.Backend.Core.Middleware.AuthenticationMiddleware;
using AuthorizationMiddleware = Iceshrimp.Backend.Core.Middleware.AuthorizationMiddleware;
using NoteRenderer = Iceshrimp.Backend.Controllers.Renderers.NoteRenderer;
using NotificationRenderer = Iceshrimp.Backend.Controllers.Renderers.NotificationRenderer;
using UserRenderer = Iceshrimp.Backend.Controllers.Renderers.UserRenderer;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ServiceExtensions
{
	public static void AddServices(this IServiceCollection services)
	{
		// Transient = instantiated per request and class

		// Scoped = instantiated per request
		services
			.AddScoped<ActivityPub.ActivityRenderer>()
			.AddScoped<ActivityPub.UserRenderer>()
			.AddScoped<ActivityPub.NoteRenderer>()
			.AddScoped<ActivityPub.UserResolver>()
			.AddScoped<ActivityPub.ObjectResolver>()
			.AddScoped<ActivityPub.MentionsResolver>()
			.AddScoped<ActivityPub.ActivityDeliverService>()
			.AddScoped<ActivityPub.FederationControlService>()
			.AddScoped<ActivityPub.ActivityHandlerService>()
			.AddScoped<ActivityPub.ActivityFetcherService>()
			.AddScoped<UserService>()
			.AddScoped<NoteService>()
			.AddScoped<EmojiService>()
			.AddScoped<WebFingerService>()
			.AddScoped<SystemUserService>()
			.AddScoped<DriveService>()
			.AddScoped<NotificationService>()
			.AddScoped<DatabaseMaintenanceService>()
			.AddScoped<UserProfileMentionsResolver>()
			.AddScoped<AuthorizedFetchMiddleware>()
			.AddScoped<InboxValidationMiddleware>()
			.AddScoped<AuthenticationMiddleware>()
			.AddScoped<ErrorHandlerMiddleware>()
			.AddScoped<Controllers.Mastodon.Renderers.UserRenderer>()
			.AddScoped<Controllers.Mastodon.Renderers.NoteRenderer>()
			.AddScoped<Controllers.Mastodon.Renderers.NotificationRenderer>()
			.AddScoped<PollRenderer>()
			.AddScoped<PollService>()
			.AddScoped<NoteRenderer>()
			.AddScoped<UserRenderer>()
			.AddScoped<NotificationRenderer>()
			.AddScoped<ActivityPubController>()
			.AddScoped<FollowupTaskService>()
			.AddScoped<InstanceService>()
			.AddScoped<MfmConverter>()
			.AddScoped<UserProfileRenderer>()
			.AddScoped<CacheService>()
			.AddScoped<MetaService>();

		// Singleton = instantiated once across application lifetime
		services
			.AddSingleton<HttpClient, CustomHttpClient>()
			.AddSingleton<HttpRequestService>()
			.AddSingleton<CronService>()
			.AddSingleton<QueueService>()
			.AddSingleton<ObjectStorageService>()
			.AddSingleton<EventService>()
			.AddSingleton<RequestBufferingMiddleware>()
			.AddSingleton<AuthorizationMiddleware>()
			.AddSingleton<RequestVerificationMiddleware>()
			.AddSingleton<RequestDurationMiddleware>()
			.AddSingleton<PushService>()
			.AddSingleton<StreamingService>();

		// Hosted services = long running background tasks
		// Note: These need to be added as a singleton as well to ensure data consistency
		services.AddHostedService<CronService>(provider => provider.GetRequiredService<CronService>());
		services.AddHostedService<QueueService>(provider => provider.GetRequiredService<QueueService>());
		services.AddHostedService<PushService>(provider => provider.GetRequiredService<PushService>());
	}

	public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
	{
		services.ConfigureWithValidation<Config>(configuration)
		        .ConfigureWithValidation<Config.InstanceSection>(configuration, "Instance")
		        .ConfigureWithValidation<Config.WorkerSection>(configuration, "Worker")
		        .ConfigureWithValidation<Config.SecuritySection>(configuration, "Security")
		        .ConfigureWithValidation<Config.DatabaseSection>(configuration, "Database")
		        .ConfigureWithValidation<Config.StorageSection>(configuration, "Storage")
		        .ConfigureWithValidation<Config.LocalStorageSection>(configuration, "Storage:Local")
		        .ConfigureWithValidation<Config.ObjectStorageSection>(configuration, "Storage:ObjectStorage");
	}

	private static IServiceCollection ConfigureWithValidation<T>(
		this IServiceCollection services, IConfiguration config
	) where T : class
	{
		services.AddOptionsWithValidateOnStart<T>()
		        .Bind(config)
		        .ValidateDataAnnotations();

		return services;
	}

	private static IServiceCollection ConfigureWithValidation<T>(
		this IServiceCollection services, IConfiguration config, string name
	) where T : class
	{
		services.AddOptionsWithValidateOnStart<T>()
		        .Bind(config.GetSection(name))
		        .ValidateDataAnnotations();

		return services;
	}

	public static void AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
	{
		var config     = configuration.GetSection("Database").Get<Config.DatabaseSection>();
		var dataSource = DatabaseContext.GetDataSource(config);
		services.AddDbContext<DatabaseContext>(options => { DatabaseContext.Configure(options, dataSource); });
		services.AddKeyedDatabaseContext<DatabaseContext>("cache");
		services.AddDataProtection()
		        .PersistKeysToDbContext<DatabaseContext>()
		        .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
		        {
			        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
			        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
		        });
	}

	private static void AddKeyedDatabaseContext<T>(
		this IServiceCollection services, string key, ServiceLifetime contextLifetime = ServiceLifetime.Scoped
	) where T : DbContext
	{
		services.TryAdd(new ServiceDescriptor(typeof(T), key, typeof(T), contextLifetime));
	}

	public static void AddSwaggerGenWithOptions(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(options =>
		{
			options.SupportNonNullableReferenceTypes();

			options.SwaggerDoc("iceshrimp", new OpenApiInfo { Title  = "Iceshrimp.NET" });
			options.SwaggerDoc("federation", new OpenApiInfo { Title = "Federation" });
			options.SwaggerDoc("mastodon", new OpenApiInfo { Title   = "Mastodon" });

			options.AddSecurityDefinition("iceshrimp",
			                              new OpenApiSecurityScheme
			                              {
				                              Name   = "Authorization token",
				                              In     = ParameterLocation.Header,
				                              Type   = SecuritySchemeType.Http,
				                              Scheme = "bearer"
			                              });
			options.AddSecurityDefinition("mastodon",
			                              new OpenApiSecurityScheme
			                              {
				                              Name   = "Authorization token",
				                              In     = ParameterLocation.Header,
				                              Type   = SecuritySchemeType.Http,
				                              Scheme = "bearer"
			                              });

			options.AddFilters();
		});
	}

	public static void AddSlidingWindowRateLimiter(this IServiceCollection services)
	{
		//TODO: separate limiter for authenticated users, partitioned by user id
		//TODO: ipv6 /64 subnet buckets
		//TODO: rate limit status headers - maybe switch to https://github.com/stefanprodan/AspNetCoreRateLimit?
		//TODO: alternatively just write our own
		services.AddRateLimiter(options =>
		{
			options.AddSlidingWindowLimiter("sliding", limiterOptions =>
			{
				limiterOptions.PermitLimit          = 500;
				limiterOptions.SegmentsPerWindow    = 60;
				limiterOptions.Window               = TimeSpan.FromSeconds(60);
				limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
				limiterOptions.QueueLimit           = 0;
			});
			options.AddSlidingWindowLimiter("strict", limiterOptions =>
			{
				limiterOptions.PermitLimit          = 10;
				limiterOptions.SegmentsPerWindow    = 60;
				limiterOptions.Window               = TimeSpan.FromSeconds(60);
				limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
				limiterOptions.QueueLimit           = 0;
			});
			options.OnRejected = async (context, token) =>
			{
				context.HttpContext.Response.StatusCode  = 429;
				context.HttpContext.Response.ContentType = "application/json";
				var res = new ErrorResponse
				{
					Error      = "Too Many Requests",
					StatusCode = 429,
					RequestId  = context.HttpContext.TraceIdentifier
				};
				await context.HttpContext.Response.WriteAsJsonAsync(res, token);
			};
		});
	}

	public static void AddCorsPolicies(this IServiceCollection services)
	{
		services.AddCors(options =>
		{
			options.AddPolicy("well-known", policy =>
			{
				policy.WithOrigins("*")
				      .WithMethods("GET")
				      .WithHeaders("Accept")
				      .WithExposedHeaders("Vary");
			});
			options.AddPolicy("drive", policy =>
			{
				policy.WithOrigins("*")
				      .WithMethods("GET", "HEAD");
			});
			options.AddPolicy("mastodon", policy =>
			{
				policy.WithOrigins("*")
				      .WithMethods("GET", "HEAD", "POST", "PUT", "PATCH", "DELETE", "CONNECT")
				      .WithHeaders("Authorization", "Content-Type", "Idempotency-Key")
				      .WithExposedHeaders("Link", "Connection", "Sec-Websocket-Accept", "Upgrade");
			});
		});
	}

	public static void AddAuthorizationPolicies(this IServiceCollection services)
	{
		services.AddAuthorizationBuilder()
		        .AddPolicy("HubAuthorization", policy =>
		        {
			        policy.Requirements.Add(new HubAuthorizationRequirement());
			        policy.AuthenticationSchemes = ["HubAuthenticationScheme"];
		        });

		services.AddAuthentication(options =>
		{
			options.AddScheme<HubAuthenticationHandler>("HubAuthenticationScheme", null);
			
			// Add a stub authentication handler to bypass strange ASP.NET Core >=7.0 defaults
			// Ref: https://github.com/dotnet/aspnetcore/issues/44661
			options.AddScheme<IAuthenticationHandler>("StubAuthenticationHandler", null);
		});
	}
}