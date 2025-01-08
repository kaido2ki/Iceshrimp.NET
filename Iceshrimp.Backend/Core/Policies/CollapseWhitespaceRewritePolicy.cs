using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Services;
using Iceshrimp.MfmSharp;

namespace Iceshrimp.Backend.Core.Policies;

public partial class CollapseWhitespaceRewritePolicy(
	bool enabled,
	int priority
) : IRewritePolicy
{
	public string                          Name         => nameof(CollapseWhitespaceRewritePolicy);
	public bool                            Enabled      => enabled;
	public int                             Priority     => priority;
	public IRewritePolicy.HookLocationEnum HookLocation => IRewritePolicy.HookLocationEnum.PostLogic;

	[GeneratedRegex(" +")] private static partial Regex WhitespaceRegex { get; }

	public void Apply(NoteService.NoteCreationData data)
	{
		if (data.Text == null || !IsApplicable(data)) return;
		data.Text       = WhitespaceRegex.Replace(data.Text, " ");
		data.ParsedText = MfmParser.Parse(data.Text);
	}

	public void Apply(NoteService.NoteUpdateData data)
	{
		if (data.Text == null || !IsApplicable(data)) return;
		data.Text       = WhitespaceRegex.Replace(data.Text, " ");
		data.ParsedText = MfmParser.Parse(data.Text);
	}

	public void Apply(ASNote note, User actor) { }

	private static bool IsApplicable(NoteService.NoteCreationData data)
		=> data.Text != null && data.Text.Contains("  ") && !data.Text.Contains("$[");

	private static bool IsApplicable(NoteService.NoteUpdateData data)
		=> data.Text != null && data.Text.Contains("  ") && !data.Text.Contains("$[");
}

public class CollapseWhitespaceRewritePolicyConfiguration : IPolicyConfiguration<CollapseWhitespaceRewritePolicy>
{
	public CollapseWhitespaceRewritePolicy Apply() => new(Enabled, Priority);
	IPolicy IPolicyConfiguration.          Apply() => Apply();

	public bool Enabled  { get; set; }
	public int  Priority { get; set; }
}
