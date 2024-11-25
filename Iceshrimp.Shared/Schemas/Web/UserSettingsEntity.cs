namespace Iceshrimp.Shared.Schemas.Web;

public class UserSettingsResponse : UserSettingsRequest
{
	public bool           TwoFactorEnrolled       { get; set; }
}

public class UserSettingsRequest
{
	public NoteVisibility DefaultNoteVisibility   { get; set; }
	public NoteVisibility DefaultRenoteVisibility { get; set; }
	public bool           PrivateMode             { get; set; }
	public bool           FilterInaccessible      { get; set; }
	public bool           AutoAcceptFollowed      { get; set; }
	public bool           AlwaysMarkSensitive     { get; set; }
}