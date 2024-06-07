namespace Iceshrimp.Backend.Core.Extensions;

public static class ConfigurationBuilderExtensions
{
	public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder configuration)
	{
		var main = Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG") ?? "configuration.ini";
		var overrides = Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG_OVERRIDES") ??
		                "configuration.overrides.ini";

		configuration.AddIniFile(main, false, true)
		             .AddIniFile(overrides, true, true)
		             .AddEnvironmentVariables();

		return configuration;
	}
}