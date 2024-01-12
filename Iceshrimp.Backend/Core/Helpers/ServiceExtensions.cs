using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Helpers;

public static class ServiceExtensions {
	public static void AddServices(this IServiceCollection services) {
		services.AddScoped<UserService>();
		services.AddScoped<NoteService>();
	}
	
	public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration) {
		//TODO: fail if config doesn't parse correctly / required things are missing
		services.Configure<Config.InstanceSection>(configuration.GetSection("Instance"));
		services.Configure<Config.DatabaseSection>(configuration.GetSection("Database"));
		services.AddScoped<Config.InstanceSection>();
		services.AddScoped<Config.DatabaseSection>();
		
		Config.StartupConfig = configuration.Get<Config>()!;
	}
}