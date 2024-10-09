using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Federation.ActivityStreams.Types;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class ForceCwRewritePolicy(
	bool enabled,
	int priority,
	string cw,
	string[] words,
	string[] instances
) : IRewritePolicy
{
	public string                          Name         => nameof(ForceCwRewritePolicy);
	public bool                            Enabled      => enabled;
	public int                             Priority     => priority;
	public IRewritePolicy.HookLocationEnum HookLocation => IRewritePolicy.HookLocationEnum.PreLogic;

	public void Apply(NoteService.NoteCreationData data)
	{
		if (IsApplicable(data))
			data.Cw = Apply(data.Cw);
	}

	public void Apply(NoteService.NoteUpdateData data)
	{
		if (IsApplicable(data))
			data.Cw = Apply(data.Cw);
	}

	public void Apply(ASNote note, User actor) { }

	private string Apply(string? oldCw)
	{
		if (oldCw == null)
			return cw;
		if (oldCw.ToLowerInvariant().Contains(cw.ToLowerInvariant()))
			return oldCw;
		return oldCw + $", {cw}";
	}

	private bool IsApplicable(NoteService.NoteCreationData data)
	{
		if (instances.Contains(data.User.Host)) return true;
		return IsApplicable([
			data.Text, data.Cw, ..data.Poll?.Choices ?? [], ..data.Attachments?.Select(p => p.Comment) ?? []
		]);
	}

	private bool IsApplicable(NoteService.NoteUpdateData data)
	{
		if (instances.Contains(data.Note.User.Host)) return true;
		return IsApplicable([
			data.Text, data.Cw, ..data.Poll?.Choices ?? [], ..data.Attachments?.Select(p => p.Comment) ?? []
		]);
	}

	private bool IsApplicable(IEnumerable<string?> candidates)
	{
		foreach (var candidate in candidates.NotNull())
		{
			foreach (var keyword in words)
			{
				if (keyword.StartsWith('"') && keyword.EndsWith('"'))
				{
					var pattern = $@"\b{EfHelpers.EscapeRegexQuery(keyword[1..^1])}\b";
					var regex   = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(10));

					if (regex.IsMatch(candidate))
						return true;
				}
				else if (candidate.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
				{
					return true;
				}
			}
		}

		return false;
	}
}

public class ForceCwRewritePolicyConfiguration : IPolicyConfiguration<ForceCwRewritePolicy>
{
	private string[]             _instances = [];
	public  ForceCwRewritePolicy Apply() => new(Enabled, Priority, Cw, Words, Instances);
	IPolicy IPolicyConfiguration.Apply() => Apply();

	public bool     Enabled  { get; set; }
	public int      Priority { get; set; }
	public string   Cw       { get; set; } = "forced content warning";
	public string[] Words    { get; set; } = [];

	public string[] Instances
	{
		get => _instances;
		set => _instances = value.Select(p => p.ToLowerInvariant()).ToArray();
	}
}