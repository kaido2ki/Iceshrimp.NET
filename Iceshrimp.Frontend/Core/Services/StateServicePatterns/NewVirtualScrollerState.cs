namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class NewVirtualScroller
{
	public Dictionary<string, NewVirtualScrollerState> States = new();
	
}

internal class NewVirtualScrollerState
{
	public required SortedDictionary<string, Child> Items   { get; set; }
	public required float              ScrollY { get; set; }

	
}
public class Child
{
	public required string     Id   { get; set; }
	public required long? Height { get; set; }
}
