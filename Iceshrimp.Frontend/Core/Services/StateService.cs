using Iceshrimp.Frontend.Core.Services.StateServicePatterns;

namespace Iceshrimp.Frontend.Core.Services;

internal class StateService(MessageService messageService)
{
	public VirtualScroller VirtualScroller { get; } = new(messageService);
	public Timeline        Timeline        { get; } = new(messageService);
	public SingleNote      SingleNote      { get; } = new();
	public Search          Search          { get; } = new();
}