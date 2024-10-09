using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class ForceMediaSensitiveRewritePolicy(
	bool enabled,
	int priority,
	string[] instances
) : IRewritePolicy
{
	public string                          Name         => nameof(ForceMediaSensitiveRewritePolicy);
	public bool                            Enabled      => enabled;
	public int                             Priority     => priority;
	public IRewritePolicy.HookLocationEnum HookLocation => IRewritePolicy.HookLocationEnum.PreLogic;

	public void Apply(NoteService.NoteCreationData data)
	{
		if (IsApplicable(data))
			Apply(data.Attachments);
	}

	public void Apply(NoteService.NoteUpdateData data)
	{
		if (IsApplicable(data))
			Apply(data.Attachments);
	}

	public void Apply(ASNote note, User actor) { }

	private void Apply(IEnumerable<DriveFile>? files)
	{
		if (files == null) return;
		foreach (var file in files) file.IsSensitive = true;
	}

	private bool IsApplicable(NoteService.NoteCreationData data) => instances.Contains(data.User.Host);
	private bool IsApplicable(NoteService.NoteUpdateData data)   => instances.Contains(data.Note.User.Host);
}

public class ForceMediaSensitivePolicyConfiguration : IPolicyConfiguration<ForceMediaSensitiveRewritePolicy>
{
	private string[]                         _instances = [];
	public  ForceMediaSensitiveRewritePolicy Apply() => new(Enabled, Priority, Instances);
	IPolicy IPolicyConfiguration.            Apply() => Apply();

	public bool Enabled  { get; set; }
	public int  Priority { get; set; }

	public string[] Instances
	{
		get => _instances;
		set => _instances = value.Select(p => p.ToLowerInvariant()).ToArray();
	}
}