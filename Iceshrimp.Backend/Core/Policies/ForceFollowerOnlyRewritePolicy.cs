using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class ForceFollowerOnlyRewritePolicy(bool enabled, int priority, string[] instances) : IRewritePolicy
{
	public string                          Name         => nameof(ForceFollowerOnlyRewritePolicy);
	public bool                            Enabled      => enabled;
	public int                             Priority     => priority;
	public IRewritePolicy.HookLocationEnum HookLocation => IRewritePolicy.HookLocationEnum.PreLogic;

	public void Apply(NoteService.NoteCreationData data)
	{
		if (IsApplicable(data))
			data.Visibility = Note.NoteVisibility.Followers;
	}

	public void Apply(NoteService.NoteUpdateData data) { }

	public void Apply(ASNote note, User actor) { }

	private bool IsApplicable(NoteService.NoteCreationData data)
	{
		return instances.Contains(data.User.Host) && data.Visibility < Note.NoteVisibility.Followers;
	}
}

public class ForceFollowerOnlyRewritePolicyConfiguration : IPolicyConfiguration<ForceFollowerOnlyRewritePolicy>
{
	private string[]                       _instances = [];
	public  ForceFollowerOnlyRewritePolicy Apply() => new(Enabled, Priority, Instances);
	IPolicy IPolicyConfiguration.          Apply() => Apply();

	public bool Enabled  { get; set; }
	public int  Priority { get; set; }

	public string[] Instances
	{
		get => _instances;
		set => _instances = value.Select(p => p.ToLowerInvariant()).ToArray();
	}
}