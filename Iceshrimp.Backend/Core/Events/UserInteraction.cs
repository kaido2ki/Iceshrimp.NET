using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Core.Events;

public class UserInteraction
{
	public required User Actor;
	public required User Object;
}