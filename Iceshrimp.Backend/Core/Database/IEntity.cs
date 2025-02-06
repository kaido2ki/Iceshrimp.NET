using Iceshrimp.Shared.Helpers;

namespace Iceshrimp.Backend.Core.Database;

public interface IEntity : IIdentifiable;

public class EntityWrapper<T> : IEntity
{
	public required T      Entity { get; init; }
	public required string Id     { get; init; }
}
