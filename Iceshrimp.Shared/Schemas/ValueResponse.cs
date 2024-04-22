namespace Iceshrimp.Shared.Schemas;

public class ValueResponse(long count)
{
	public long Value { get; set; } = count;
}