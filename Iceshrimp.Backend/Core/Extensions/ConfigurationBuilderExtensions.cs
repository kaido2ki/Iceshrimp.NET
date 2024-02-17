namespace Iceshrimp.Backend.Core.Extensions;

public static class ConfigurationBuilderExtensions
{
	public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder configuration)
	{
		return configuration.AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG") ?? "configuration.ini",
		                                false, true)
		                    .AddIniFile(Environment.GetEnvironmentVariable("ICESHRIMP_CONFIG_OVERRIDES") ?? "configuration.overrides.ini",
		                                true, true);
	}
}