using Iceshrimp.Frontend.Core.Services.StateServicePatterns;

namespace Iceshrimp.Frontend.Core.Services;

internal class StateService
{
	public SingleNote         SingleNote         { get; } = new();
	public Search             Search             { get; } = new();
	public NewVirtualScroller NewVirtualScroller { get; } = new();
}