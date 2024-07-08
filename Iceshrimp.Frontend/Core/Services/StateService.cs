using Iceshrimp.Frontend.Core.Services.StateServicePatterns;

namespace Iceshrimp.Frontend.Core.Services;

internal class StateService(MessageService messageService)
{
	public VirtualScroller VirtualScroller { get; } = new();
	public Timeline        Timeline        { get; } = new(messageService);
}