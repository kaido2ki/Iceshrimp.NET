using Iceshrimp.Backend.Core.Services;

namespace Iceshrimp.Backend.Core.Policies;

public class UserAgeRejectPolicy(bool enabled, TimeSpan age) : IRejectPolicy
{
	public string Name    => nameof(UserAgeRejectPolicy);
	public bool   Enabled => enabled;

	public bool ShouldReject(NoteService.NoteCreationData data) => DateTime.Now - data.User.CreatedAt < age;
}

public class UserAgeRejectPolicyConfiguration : IPolicyConfiguration<UserAgeRejectPolicy>
{
	public UserAgeRejectPolicy   Apply() => new(Enabled, Age);
	IPolicy IPolicyConfiguration.Apply() => Apply();

	public required bool     Enabled { get; set; }
	public required TimeSpan Age     { get; set; }
}