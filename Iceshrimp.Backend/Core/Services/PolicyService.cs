using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Iceshrimp.AssemblyUtils;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Core.Services;

public class PolicyService(IServiceScopeFactory scopeFactory)
{
	private bool             _initialized;
	private IRejectPolicy[]  _rejectPolicies  = [];
	private IRewritePolicy[] _rewritePolicies = [];

	private Type[] _policyTypes              = [];
	private Type[] _policyConfigurationTypes = [];

	public async Task Initialize()
	{
		if (_initialized) return;
		_initialized = true;

		var assemblies = PluginLoader.Assemblies.Prepend(Assembly.GetExecutingAssembly()).ToArray();
		_policyTypes = assemblies.SelectMany(AssemblyLoader.GetImplementationsOfInterface<IPolicy>).ToArray();
		_policyConfigurationTypes = assemblies
		                            .SelectMany(AssemblyLoader.GetImplementationsOfInterface<IPolicyConfiguration>)
		                            .Where(p => p.GetInterfaces()
		                                         .Any(i => i.GenericTypeArguments is [var t] &&
		                                                   _policyTypes.Contains(t)))
		                            .ToArray();

		await Update();
	}

	public async Task Update()
	{
		if (!_initialized) await Initialize();
		await using var scope = scopeFactory.CreateAsyncScope();
		var             db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

		var configs  = await db.PolicyConfiguration.ToArrayAsync();
		var policies = new List<IPolicy>();
		foreach (var config in configs)
		{
			var match = _policyConfigurationTypes
				.FirstOrDefault(p => p.GetInterfaces()
				                      .FirstOrDefault(i => i.Name == typeof(IPolicyConfiguration<>).Name)
				                      ?.GenericTypeArguments.FirstOrDefault()
				                      ?.Name ==
				                     config.Name);

			if (match == null) continue;
			var deserialized =
				JsonSerializer.Deserialize(config.Data, match, JsonSerialization.Options) as IPolicyConfiguration;
			if (deserialized?.Apply() is { } policy) policies.Add(policy);
		}

		_rejectPolicies  = policies.OfType<IRejectPolicy>().ToArray();
		_rewritePolicies = policies.OfType<IRewritePolicy>().ToArray();
	}

	public bool ShouldReject(NoteService.NoteCreationData data, [NotNullWhen(true)] out IRejectPolicy? policy)
	{
		policy = _rejectPolicies.FirstOrDefault(p => p.Enabled && p.ShouldReject(data));
		return policy != null;
	}

	public void CallRewriteHooks(NoteService.NoteCreationData data, IRewritePolicy.HookLocationEnum location)
	{
		var hooks = _rewritePolicies.Where(p => p.Enabled && p.HookLocation == location)
		                            .OrderByDescending(p => p.Priority);

		foreach (var hook in hooks) hook.Apply(data);
	}

	public void CallRewriteHooks(NoteService.NoteUpdateData data, IRewritePolicy.HookLocationEnum location)
	{
		var hooks = _rewritePolicies.Where(p => p.Enabled && p.HookLocation == location)
		                            .OrderByDescending(p => p.Priority);

		foreach (var hook in hooks) hook.Apply(data);
	}

	public void CallRewriteHooks(ASNote note, User actor, IRewritePolicy.HookLocationEnum location)
	{
		var hooks = _rewritePolicies.Where(p => p.Enabled && p.HookLocation == location)
		                            .OrderByDescending(p => p.Priority);

		foreach (var hook in hooks) hook.Apply(note, actor);
	}

	public async Task<Type?> GetConfigurationType(string name)
	{
		await Initialize();
		var type = _policyTypes.FirstOrDefault(p => p.Name == name);
		return _policyConfigurationTypes
			.FirstOrDefault(p => p.GetInterfaces()
			                      .FirstOrDefault(i => i.Name == typeof(IPolicyConfiguration<>).Name)
			                      ?.GenericTypeArguments.FirstOrDefault() ==
			                     type);
	}

	public async Task<IPolicyConfiguration?> GetConfiguration(string name, string? data)
	{
		var type = await GetConfigurationType(name);
		if (type == null) return null;
		if (data == null) return (IPolicyConfiguration?)Activator.CreateInstance(type);
		return (IPolicyConfiguration?)JsonSerializer.Deserialize(data, type, JsonSerialization.Options);
	}

	public async Task<List<string>> GetAvailablePolicies()
	{
		await Initialize();
		return _policyTypes.Select(p => p.Name).ToList();
	}
}

public interface IPolicy
{
	public string Name    { get; }
	public bool   Enabled { get; }
}

public interface IRewritePolicy : IPolicy
{
	public enum HookLocationEnum
	{
		PreLogic,
		PostLogic
	}

	public int              Priority     { get; }
	public HookLocationEnum HookLocation { get; }

	public void Apply(NoteService.NoteCreationData data);
	public void Apply(NoteService.NoteUpdateData data);
	public void Apply(ASNote note, User actor);
}

public interface IRejectPolicy : IPolicy
{
	public bool ShouldReject(NoteService.NoteCreationData data);
	//TODO: reject during note update & note process
}

public interface IPolicyConfiguration
{
	public bool    Enabled { get; }
	public IPolicy Apply();
}

public interface IPolicyConfiguration<out TPolicy> : IPolicyConfiguration where TPolicy : IPolicy
{
	public new TPolicy Apply();
}