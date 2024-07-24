using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Events;

public class NoteInteraction
{
	public required Note Note { get; init; }
	public required User User { get; init; }
}