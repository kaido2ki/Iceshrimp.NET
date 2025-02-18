namespace Iceshrimp.Shared.Helpers;

public interface IIdentifiable
{
	public string Id { get; }
}

public class EntityWrapper<T> : IIdentifiable
{
	public required T      Entity { get; init; }
	public required string Id     { get; init; }
}

