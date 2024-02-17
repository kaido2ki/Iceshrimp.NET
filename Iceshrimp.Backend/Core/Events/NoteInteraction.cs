using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Events;

public class NoteInteraction
{
	public required Note Note;
	public required User User;
}