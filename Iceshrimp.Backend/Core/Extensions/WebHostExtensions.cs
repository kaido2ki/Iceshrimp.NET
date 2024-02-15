using Iceshrimp.Backend.Core.Configuration;

namespace Iceshrimp.Backend.Core.Extensions;

public static class WebHostExtensions {
	public static void ConfigureKestrel(this IWebHostBuilder builder, IConfiguration configuration) {
		var config = configuration.GetSection("Instance").Get<Config.InstanceSection>() ??
		             throw new Exception("Failed to read Instance config section");

		if (config.ListenSocket == null) return;

		if (File.Exists(config.ListenSocket))
			throw new Exception($"Failed to configure unix socket {config.ListenSocket}: File exists");
		if (!Path.Exists(Path.GetDirectoryName(config.ListenSocket)))
			throw new Exception($"Failed to configure unix socket {config.ListenSocket}: Directory does not exist");

		builder.ConfigureKestrel(options => options.ListenUnixSocket(config.ListenSocket));
	}
}