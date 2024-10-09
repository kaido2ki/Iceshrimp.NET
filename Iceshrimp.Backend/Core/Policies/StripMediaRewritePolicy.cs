using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class StripMediaRewritePolicy(
	bool enabled,
	int priority,
	string[] instances
) : IRewritePolicy
{
	public string                          Name         => nameof(StripMediaRewritePolicy);
	public bool                            Enabled      => enabled;
	public int                             Priority     => priority;
	public IRewritePolicy.HookLocationEnum HookLocation => IRewritePolicy.HookLocationEnum.PreLogic;

	public void Apply(NoteService.NoteCreationData data)
	{
		if (data.User.Host == null) return;
		if (IsApplicable(data.User.Host))
			data.Attachments = [];
	}

	public void Apply(NoteService.NoteUpdateData data)
	{
		if (data.Note.User.Host == null) return;
		if (IsApplicable(data.Note.User.Host))
			data.Attachments = [];
	}

	public void Apply(ASNote note, User actor)
	{
		if (actor.Host == null) return;
		if (IsApplicable(actor.Host))
			note.Attachments = [];
	}

	private bool IsApplicable(string host) => instances.Contains(host);
}

public class StripMediaRewritePolicyConfiguration : IPolicyConfiguration<StripMediaRewritePolicy>
{
	private string[]                _instances = [];
	public  StripMediaRewritePolicy Apply() => new(Enabled, Priority, Instances);
	IPolicy IPolicyConfiguration.   Apply() => Apply();

	public bool Enabled  { get; set; }
	public int  Priority { get; set; }

	public string[] Instances
	{
		get => _instances;
		set => _instances = value.Select(p => p.ToLowerInvariant()).ToArray();
	}
}