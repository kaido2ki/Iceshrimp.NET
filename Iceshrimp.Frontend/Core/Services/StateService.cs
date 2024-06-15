
using Iceshrimp.Frontend.Core.Services.StateServicePatterns;

namespace Iceshrimp.Frontend.Core.Services;

public class StateService
{
    public VirtualScroller VirtualScroller { get; } = new();
    public Timeline        Timeline        { get; } = new();

}