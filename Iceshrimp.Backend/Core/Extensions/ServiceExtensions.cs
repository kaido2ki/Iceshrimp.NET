using Iceshrimp.Backend.Controllers.Renderers.ActivityPub;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Federation.ActivityPub;
using Iceshrimp.Backend.Core.Federation.WebFinger;
using Iceshrimp.Backend.Core.Middleware;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Extensions;

public static class ServiceExtensions {
	public static void AddServices(this IServiceCollection services) {
		// Transient = instantiated per request and class
		//services.AddTransient<T>();

		// Scoped = instantiated per request
		services.AddScoped<UserResolver>();
		services.AddScoped<UserService>();
		services.AddScoped<NoteService>();
		services.AddScoped<UserRenderer>();
		services.AddScoped<NoteRenderer>();
		services.AddScoped<WebFingerService>();
		services.AddScoped<AuthorizedFetchMiddleware>();

		// Singleton = instantiated once across application lifetime
		services.AddSingleton<HttpClient>();
		services.AddSingleton<HttpRequestService>();
		services.AddSingleton<ActivityPubService>();
		services.AddSingleton<ErrorHandlerMiddleware>();
		services.AddSingleton<RequestBufferingMiddleware>();

		// Hosted services = long running background tasks
		services.AddHostedService<QueueService>();
	}

	public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration) {
		//TODO: fail if config doesn't parse correctly / required things are missing
		services.Configure<Config>(configuration);
		services.Configure<Config.InstanceSection>(configuration.GetSection("Instance"));
		services.Configure<Config.SecuritySection>(configuration.GetSection("Security"));
		services.Configure<Config.DatabaseSection>(configuration.GetSection("Database"));
	}
}