namespace Iceshrimp.Shared.Schemas.Web;

public class ValueResponse(long count)
{
	public long Value { get; set; } = count;
}