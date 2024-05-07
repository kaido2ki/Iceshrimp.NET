namespace Iceshrimp.Shared.Schemas;

public class UserSettingsEntity
{
	public NoteVisibility DefaultNoteVisibility   { get; set; }
	public NoteVisibility DefaultRenoteVisibility { get; set; }
	public bool           PrivateMode             { get; set; }
	public bool           FilterInaccessible      { get; set; }
}