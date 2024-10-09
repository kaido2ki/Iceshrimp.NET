using System.Text.RegularExpressions;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;
using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class WordRejectPolicy(bool enabled, string[] words) : IRejectPolicy
{
	public string Name    => nameof(WordRejectPolicy);
	public bool   Enabled => enabled;

	public bool ShouldReject(NoteService.NoteCreationData data)
	{
		List<string?> candidates =
		[
			data.Text, data.Cw, ..data.Poll?.Choices ?? [], ..data.Attachments?.Select(p => p.Comment) ?? []
		];

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

public class WordRejectPolicyConfiguration : IPolicyConfiguration<WordRejectPolicy>
{
	public WordRejectPolicy      Apply() => new(Enabled, Words);
	IPolicy IPolicyConfiguration.Apply() => Apply();

	public required bool     Enabled { get; set; }
	public required string[] Words   { get; set; }
}