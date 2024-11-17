using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.RateLimiting;
using System.Xml.Linq;
using Iceshrimp.AssemblyUtils;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.SignalR.Authentication;
using Iceshrimp.Shared.Configuration;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ServiceExtensions
{
	public static void AddServices(this IServiceCollection services, IConfiguration configuration)
	{
		var config = configuration.Get<Config>() ?? throw new Exception("Failed to read storage config section");

		var serviceTypes = PluginLoader
		                   .Assemblies.Prepend(Assembly.GetExecutingAssembly())
		                   .SelectMany(AssemblyLoader.GetImplementationsOfInterface<IService>)
		                   .OrderBy(type => type.GetInterfaceProperty<IService, int?>(nameof(IService.Priority)) ?? 0)
		                   .ToArray();

		foreach (var type in serviceTypes)
		{
			if (type.GetInterfaceProperty<IService, ServiceLifetime?>(nameof(IService.Lifetime)) is not { } lifetime)
				continue;

			if (type.GetInterface(nameof(IConditionalService)) != null)
				if (type.CallInterfaceMethod(nameof(IConditionalService.Predicate), config) is not true)
					continue;

			var serviceType = type.GetInterfaceProperty<IService, Type>(nameof(IService.ServiceType)) ?? type;
			services.Add(new ServiceDescriptor(serviceType, type, lifetime));
		}

		var hostedServiceTypes = PluginLoader
		                         .Assemblies.Prepend(Assembly.GetExecutingAssembly())
		                         .SelectMany(AssemblyLoader.GetImplementationsOfInterface<IHostedService>)
		                         .ToArray();

		foreach (var type in hostedServiceTypes)
		{
			if (type.GetInterface(nameof(IService)) == null)
				services.Add(new ServiceDescriptor(type, type, ServiceLifetime.Singleton));

			services.Add(new ServiceDescriptor(typeof(IHostedService), provider => provider.GetRequiredService(type),
			                                   ServiceLifetime.Singleton));
		}
	}

	public static void AddMiddleware(this IServiceCollection services)
	{
		var types = PluginLoader
		            .Assemblies.Prepend(Assembly.GetExecutingAssembly())
		            .SelectMany(p => AssemblyLoader.GetImplementationsOfInterface(p, typeof(IMiddlewareService)));

		foreach (var type in types)
		{
			if (type.GetProperty(nameof(IMiddlewareService.Lifetime))?.GetValue(null) is not ServiceLifetime lifetime)
				continue;

			services.Add(new ServiceDescriptor(type, type, lifetime));
		}
	}

	public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// @formatter:off
		services.ConfigureWithValidation<Config>(configuration)
		        .ConfigureWithValidation<Config.InstanceSection>(configuration, "Instance")
		        .ConfigureWithValidation<Config.SecuritySection>(configuration, "Security")
		        .ConfigureWithValidation<Config.PerformanceSection>(configuration, "Performance")
		        .ConfigureWithValidation<Config.QueueConcurrencySection>(configuration, "Performance:QueueConcurrency")
		        .ConfigureWithValidation<Config.BackfillSection>(configuration, "Backfill")
		        .ConfigureWithValidation<Config.BackfillRepliesSection>(configuration, "Backfill:Replies")
		        .ConfigureWithValidation<Config.QueueSection>(configuration, "Queue")
		        .ConfigureWithValidation<Config.JobRetentionSection>(configuration, "Queue:JobRetention")
		        .ConfigureWithValidation<Config.DatabaseSection>(configuration, "Database")
		        .ConfigureWithValidation<Config.StorageSection>(configuration, "Storage")
		        .ConfigureWithValidation<Config.LocalStorageSection>(configuration, "Storage:Local")
		        .ConfigureWithValidation<Config.ObjectStorageSection>(configuration, "Storage:ObjectStorage")
		        .ConfigureWithValidation<Config.MediaProcessingSection>(configuration, "Storage:MediaProcessing")
		        .ConfigureWithValidation<Config.ImagePipelineSection>(configuration, "Storage:MediaProcessing:ImagePipeline")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Original:Local")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Original:Remote")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Thumbnail:Local")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Thumbnail:Remote")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Public:Local")
		        .ConfigureWithValidation<Config.ImageFormatConfiguration>(configuration, "Storage:MediaProcessing:ImagePipeline:Public:Remote");
		// @formatter:on

		services.Configure<JsonOptions>(options =>
		{
			options.SerializerOptions.PropertyNamingPolicy = JsonSerialization.Options.PropertyNamingPolicy;
			foreach (var converter in JsonSerialization.Options.Converters)
				options.SerializerOptions.Converters.Add(converter);
		});

		services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
		{
			options.JsonSerializerOptions.PropertyNamingPolicy = JsonSerialization.Options.PropertyNamingPolicy;
			options.JsonSerializerOptions.MaxDepth             = 256;
			foreach (var converter in JsonSerialization.Options.Converters)
				options.JsonSerializerOptions.Converters.Add(converter);
		});

		services.PostConfigure<RazorComponentsServiceOptions>(BlazorSsrHandoffMiddleware.DisableBlazorJsInitializers);
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
		var config = configuration.GetSection("Database").Get<Config.DatabaseSection>() ??
		             throw new Exception("Failed to initialize database: Failed to load configuration");

		var dataSource = DatabaseContext.GetDataSource(config);
		services.AddDbContext<DatabaseContext>(options => { DatabaseContext.Configure(options, dataSource, config); });
		services.AddKeyedDatabaseContext<DatabaseContext>("cache");
		services.AddDataProtection()
		        .PersistKeysToDbContextAsync<DatabaseContext>()
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

	public static void AddOpenApiWithOptions(this IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(options =>
		{
			options.SupportNonNullableReferenceTypes();

			var version = new Config.InstanceSection().Version;
			options.SwaggerDoc("iceshrimp", new OpenApiInfo { Title  = "Iceshrimp.NET", Version = version });
			options.SwaggerDoc("federation", new OpenApiInfo { Title = "Federation", Version    = version });
			options.SwaggerDoc("mastodon", new OpenApiInfo { Title   = "Mastodon", Version      = version });

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
		//TODO: rate limit status headers - maybe switch to https://github.com/stefanprodan/AspNetCoreRateLimit?
		//TODO: alternatively just write our own
		services.AddRateLimiter(options =>
		{
			var sliding = new SlidingWindowRateLimiterOptions
			{
				PermitLimit          = 500,
				SegmentsPerWindow    = 60,
				Window               = TimeSpan.FromSeconds(60),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit           = 0
			};

			var auth = new SlidingWindowRateLimiterOptions
			{
				PermitLimit          = 10,
				SegmentsPerWindow    = 60,
				Window               = TimeSpan.FromSeconds(60),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit           = 0
			};

			var strict = new SlidingWindowRateLimiterOptions
			{
				PermitLimit          = 3,
				SegmentsPerWindow    = 60,
				Window               = TimeSpan.FromSeconds(60),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit           = 0
			};

			var imports = new SlidingWindowRateLimiterOptions
			{
				PermitLimit          = 2,
				SegmentsPerWindow    = 30,
				Window               = TimeSpan.FromMinutes(30),
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit           = 0
			};

			// @formatter:off
			options.AddPolicy("sliding", ctx => RateLimitPartition.GetSlidingWindowLimiter(ctx.GetRateLimitPartition(false),_ => sliding));
			options.AddPolicy("auth", ctx => RateLimitPartition.GetSlidingWindowLimiter(ctx.GetRateLimitPartition(false), _ => auth));
			options.AddPolicy("strict", ctx => RateLimitPartition.GetSlidingWindowLimiter(ctx.GetRateLimitPartition(true), _ => strict));
			options.AddPolicy("imports", ctx => RateLimitPartition.GetSlidingWindowLimiter(ctx.GetRateLimitPartition(true), _ => imports));
			// @formatter:on

			options.OnRejected = async (context, token) =>
			{
				context.HttpContext.Response.StatusCode  = 429;
				context.HttpContext.Response.ContentType = "application/json";
				var res = new ErrorResponse(new Exception())
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
			options.AddPolicy("fallback", policy =>
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

	public static void AddOutputCacheWithOptions(this IServiceCollection services)
	{
		services.AddOutputCache(options =>
		{
			options.AddPolicy("conditional", o => o.With(ctx => ctx.HttpContext.ShouldCacheOutput()));
			options.AddPolicy("federation", o => o.SetVaryByHeader("Accept").Expire(TimeSpan.FromSeconds(60)));
			options.DefaultExpirationTimeSpan = TimeSpan.FromDays(365);
		});
	}
}

public static partial class HttpContextExtensions
{
	private const string CacheKey = "shouldCache";

	public static string GetRateLimitPartition(this HttpContext ctx, bool includeRoute) =>
		(includeRoute ? ctx.Request.Path.ToString() + "#" : "") + (GetRateLimitPartitionInternal(ctx) ?? "");

	private static string? GetRateLimitPartitionInternal(this HttpContext ctx) =>
		ctx.GetUser()?.Id ??
		ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
		ctx.Connection.RemoteIpAddress?.ToString();

	public static void CacheOutput(this HttpContext ctx) => ctx.Items[CacheKey] = true;

	public static bool ShouldCacheOutput(this HttpContext ctx) =>
		ctx.Items.TryGetValue(CacheKey, out var s) && s is true;
}

public interface IService
{
	// This should be abstract instead of virtual but the runtime team said https://github.com/dotnet/runtime/issues/79331
	public static virtual ServiceLifetime Lifetime => throw new Exception("Missing IService.Lifetime override");

	public static virtual Type? ServiceType => null;
	public static virtual int   Priority    => 0;
}

/// <summary>
/// Instantiated per request and class
/// </summary>
public interface ITransientService : IService
{
	static ServiceLifetime IService.Lifetime => ServiceLifetime.Transient;
}

/// <summary>
/// Instantiated per request
/// </summary>
public interface IScopedService : IService
{
	static ServiceLifetime IService.Lifetime => ServiceLifetime.Scoped;
}

/// <summary>
/// Instantiated once across application lifetime
/// </summary>
public interface ISingletonService : IService
{
	static ServiceLifetime IService.Lifetime => ServiceLifetime.Singleton;
}

public interface IService<TService> : IService
{
	static Type IService.ServiceType => typeof(TService);
}

public interface IConditionalService : IService
{
	// This should be abstract instead of virtual but the runtime team said https://github.com/dotnet/runtime/issues/79331
	public static virtual bool Predicate(Config ctx) =>
		throw new Exception("Missing IConditionalService.Predicate override");
}

#region AsyncDataProtection handlers

/// <summary>
///     Async equivalent of EntityFrameworkCoreDataProtectionExtensions.PersistKeysToDbContext.
///     Required because Npgsql doesn't support the non-async APIs when using connection multiplexing, and the stock
///     version EFCore API calls their blocking equivalents.
/// </summary>
file static class DataProtectionExtensions
{
	public static IDataProtectionBuilder PersistKeysToDbContextAsync<TContext>(this IDataProtectionBuilder builder)
		where TContext : DbContext, IDataProtectionKeyContext
	{
		builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
		{
			var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
			return new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository =
				                                                  new EntityFrameworkCoreXmlRepositoryAsync<
					                                                  TContext>(services, loggerFactory));
		});
		return builder;
	}
}

file sealed class EntityFrameworkCoreXmlRepositoryAsync<TContext> : IXmlRepository
	where TContext : DbContext, IDataProtectionKeyContext
{
	private readonly IServiceProvider _services;

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(DataProtectionKey))]
	public EntityFrameworkCoreXmlRepositoryAsync(IServiceProvider services, ILoggerFactory loggerFactory)
	{
		ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
		_services = services ?? throw new ArgumentNullException(nameof(services));
	}

	public IReadOnlyCollection<XElement> GetAllElements()
	{
		return GetAllElementsCore().ToBlockingEnumerable().ToList().AsReadOnly();

		async IAsyncEnumerable<XElement> GetAllElementsCore()
		{
			using var scope = _services.CreateScope();
			var @enum = scope.ServiceProvider.GetRequiredService<TContext>()
			                 .DataProtectionKeys
			                 .AsNoTracking()
			                 .AsAsyncEnumerable();

			await foreach (var dataProtectionKey in @enum)
			{
				if (!string.IsNullOrEmpty(dataProtectionKey.Xml))
					yield return XElement.Parse(dataProtectionKey.Xml);
			}
		}
	}

	public void StoreElement(XElement element, string friendlyName)
	{
		using var scope           = _services.CreateAsyncScope();
		using var requiredService = scope.ServiceProvider.GetRequiredService<TContext>();
		requiredService.DataProtectionKeys.Add(new DataProtectionKey
		{
			FriendlyName = friendlyName,
			Xml          = element.ToString(SaveOptions.DisableFormatting)
		});
		requiredService.SaveChangesAsync().Wait();
	}
}

#endregion