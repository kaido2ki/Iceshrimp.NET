using Iceshrimp.Frontend.Core.Services.NoteStore;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class TimelinePage
{
	public TimelineStore.Timeline? Current { get; set; }
}
