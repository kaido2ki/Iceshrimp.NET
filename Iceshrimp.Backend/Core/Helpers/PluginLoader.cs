using System.Reflection;
using Iceshrimp.Backend.Core.Extensions;
using Weikio.PluginFramework.Abstractions;
using Weikio.PluginFramework.Catalogs;

namespace Iceshrimp.Backend.Core.Helpers;

using LoadedPlugin = (Plugin descriptor, IPlugin instance);

public class PluginLoader
{
	// Increment whenever a breaking plugin API change is made
	private const int ApiVersion = 1;

	public PluginLoader()
	{
		var path = Environment.GetEnvironmentVariable("ICESHRIMP_PLUGIN_DIR");
		path  ??= Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "plugins");
		_path =   Path.Combine(path, $"v{ApiVersion}");
	}

	private readonly string                _path;
	private          List<LoadedPlugin>    _loaded = [];
	private          IEnumerable<IPlugin>  Plugins    => _loaded.Select(p => p.instance);
	public           IEnumerable<Assembly> Assemblies => _loaded.Select(p => p.descriptor.Assembly);

	public async Task LoadPlugins()
	{
		if (!Directory.Exists(_path)) return;
		var dlls = Directory.EnumerateFiles(_path, "*.dll").ToList();
		var catalogs = dlls
		               .Select(p => new AssemblyPluginCatalog(p, type => type.Implements<IPlugin>()))
		               .Cast<IPluginCatalog>()
		               .ToArray();

		var combined = new CompositePluginCatalog(catalogs);
		await combined.Initialize();
		_loaded = combined.GetPlugins()
		                  .Select(p => (descriptor: p, instance: Activator.CreateInstance(p) as IPlugin))
		                  .Where(p => p.instance is not null)
		                  .Cast<LoadedPlugin>()
		                  .ToList();

		await Plugins.Select(i => i.Initialize()).AwaitAllNoConcurrencyAsync();
	}

	public void RunBuilderHooks(WebApplicationBuilder builder)
	{
		foreach (var plugin in Plugins) plugin.BuilderHook(builder);
	}

	public void RunAppHooks(WebApplication app)
	{
		foreach (var plugin in Plugins) plugin.AppHook(app);
	}

	public void PrintPluginInformation(WebApplication app)
	{
		var logger = app.Services.GetRequiredService<ILogger<PluginLoader>>();
		if (_loaded.Count == 0)
		{
			logger.LogInformation("Found {count} plugins in {dllPath}.", _loaded.Count, _path);
			return;
		}

		var plugins = _loaded.Select(plugin => $"{plugin.instance.Name} v{plugin.instance.Version} " +
		                                       $"({Path.GetFileName(plugin.descriptor.Assembly.Location)})");
		logger.LogInformation("Loaded {count} plugins from {dllPath}: \n* {files}", _loaded.Count, _path,
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
	public string Name    { get; }
	public string Version { get; }

	public Task                  Initialize()                               => Task.CompletedTask;
	public WebApplicationBuilder BuilderHook(WebApplicationBuilder builder) => builder;
	public WebApplication        AppHook(WebApplication app)                => app;
}