using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Events;

public class UserInteraction
{
	public required User Actor  { get; init; }
	public required User Object { get; init; }
}