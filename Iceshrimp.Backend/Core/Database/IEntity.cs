namespace Iceshrimp.Backend.Core.Database;

public interface IEntity
{
	public string Id { get; }
}

public class EntityWrapper<T> : IEntity
{
	public required T      Entity { get; init; }
	public required string Id     { get; init; }
}