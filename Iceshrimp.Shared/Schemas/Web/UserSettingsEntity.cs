namespace Iceshrimp.Shared.Schemas.Web;

public class UserSettingsResponse : UserSettingsRequest
{
	public bool           TwoFactorEnrolled       { get; set; }
}

public class UserSettingsRequest
{
	public NoteVisibility DefaultNoteVisibility   { get; set; }
	public NoteVisibility DefaultRenoteVisibility { get; set; }
	public string?        TranslationLanguage     { get; set; }
	public BiteControl    CanBite                 { get; set; }

	/// <summary>
	/// Overrides manually accept follow request to <c>true</c> and note/renote visibility to followers-only or lower.
	/// </summary>
	public bool PrivateMode { get; set; }

	/// <summary>
	/// Domains hosting pages this user can be attributed to (see https://blog.joinmastodon.org/2024/07/highlighting-journalism-on-mastodon/).
	/// </summary>
	public List<string> AttributionDomains { get; set; } = [];

	public bool FilterInaccessible      { get; set; }
	public bool AutoAcceptFollowed      { get; set; }
	public bool AlwaysMarkSensitive     { get; set; }
	public bool ManuallyAcceptFollows   { get; set; }
	public bool HideRepliesNotFollowing { get; set; }
	public bool IsExplorable            { get; set; }
}

public enum BiteControl
{
	Public,
	Followers,
	None
}
