using System.Reflection;
using Iceshrimp.AssemblyUtils;
using Iceshrimp.Backend.Core.Extensions;

namespace Iceshrimp.Backend.Core.Helpers;

using LoadedPlugin = (Assembly assembly, IPlugin instance);

public abstract class PluginLoader
{
	// Increment whenever a breaking plugin API change is made
	private const int ApiVersion = 1;

	private static readonly string PathRoot = Environment.GetEnvironmentVariable("ICESHRIMP_PLUGIN_DIR") ??
	                                          Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
	                                                       "plugins");

	private static readonly string DllPath = Path.Combine(PathRoot, $"v{ApiVersion}");

	private static List<LoadedPlugin>    _loaded = [];
	private static IEnumerable<IPlugin>  Plugins    => _loaded.Select(p => p.instance);
	public static  IEnumerable<Assembly> Assemblies => _loaded.Select(p => p.assembly);

	public static async Task LoadPlugins()
	{
		if (!Directory.Exists(DllPath)) return;
		var dlls = Directory.EnumerateFiles(DllPath, "*.dll").ToList();
		_loaded = dlls.Select(Path.GetFullPath)
		              .Select(AssemblyLoader.LoadAssemblyFromPath)
		              .Select(p => (assembly: p, impls: AssemblyLoader.GetImplementationsOfInterface<IPlugin>(p)))
		              .SelectMany(p => p.impls.Select(impl => (p.assembly, Activator.CreateInstance(impl) as IPlugin)))
		              .Cast<LoadedPlugin>()
		              .ToList();

		await Plugins.Select(i => i.Initialize()).AwaitAllNoConcurrencyAsync();
	}

	public static void RunBuilderHooks(WebApplicationBuilder builder)
	{
		foreach (var plugin in Plugins) plugin.BuilderHook(builder);
	}

	public static void RunAppHooks(WebApplication app)
	{
		foreach (var plugin in Plugins) plugin.AppHook(app);
	}

	public static void PrintPluginInformation(WebApplication app)
	{
		var logger = app.Services.GetRequiredService<ILogger<PluginLoader>>();
		if (_loaded.Count == 0)
		{
			logger.LogInformation("Found {count} plugins in {dllPath}.", _loaded.Count, DllPath);
			return;
		}

		var plugins = _loaded.Select(plugin => $"{plugin.instance.Name} v{plugin.instance.Version} " +
		                                       $"({Path.GetFileName(plugin.assembly.Location)})");
		logger.LogInformation("Loaded {count} plugins from {dllPath}: \n* {files}", _loaded.Count, DllPath,
		                      string.Join("\n* ", plugins));
	}
}

public static class MvcBuilderExtensions
{
	public static IMvcBuilder AddPlugins(this IMvcBuilder builder, IEnumerable<Assembly> assemblies) =>
		assemblies.Aggregate(builder, (current, assembly) => current.AddApplicationPart(assembly));
}

public interface IPlugin
{
	public Guid   Id      { get; }
	public string Name    { get; }
	public string Version { get; }

	public Task                  Initialize()                               => Task.CompletedTask;
	public WebApplicationBuilder BuilderHook(WebApplicationBuilder builder) => builder;
	public WebApplication        AppHook(WebApplication app)                => app;
}