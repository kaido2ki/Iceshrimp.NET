using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class HellthreadRejectPolicy(bool enabled, int mentionLimit) : IRejectPolicy
{
	public string Name    => nameof(HellthreadRejectPolicy);
	public bool   Enabled => enabled;

	public bool ShouldReject(NoteService.NoteCreationData data) =>
		data.ResolvedMentions is { Mentions: var mentions } && mentions.Count > mentionLimit;
}

public class HellthreadRejectPolicyConfiguration : IPolicyConfiguration<HellthreadRejectPolicy>
{
	public HellthreadRejectPolicy Apply() => new(Enabled, MentionLimit);
	IPolicy IPolicyConfiguration. Apply() => Apply();

	public bool Enabled      { get; set; }
	public int  MentionLimit { get; set; }
}