namespace Iceshrimp.Shared.Schemas.Web;

public class UserSettingsResponse : UserSettingsRequest
{
	public bool           TwoFactorEnrolled       { get; set; }
}

public class UserSettingsRequest
{
	public NoteVisibility DefaultNoteVisibility   { get; set; }
	public NoteVisibility DefaultRenoteVisibility { get; set; }

	/// <summary>
	/// Overrides manually accept follow request to <c>true</c> and note/renote visibility to followers-only or lower.
	/// </summary>
	public bool PrivateMode { get; set; }

	public bool FilterInaccessible      { get; set; }
	public bool AutoAcceptFollowed      { get; set; }
	public bool AlwaysMarkSensitive     { get; set; }
	public bool ManuallyAcceptFollows   { get; set; }
	public bool HideRepliesNotFollowing { get; set; }
}