using Iceshrimp.Shared.Schemas;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

public class VirtualScroller
{
	private Dictionary<string, VirtualScrollerState> States { get; } = new();

	public void SetState(string id, VirtualScrollerState state)
	{
		States[id] = state;
	}

	public VirtualScrollerState GetState(string id)
	{
		States.TryGetValue(id, out var state);
		return state ?? throw new ArgumentException($"Requested state '{id}' does not exist.");
	}
}

public class VirtualScrollerState
{
	public Dictionary<string, int> Height       = new();
	public int                     PadBottom    = 0;
	public int                     PadTop       = 0;
	public List<NoteResponse>      RenderedList = [];
	public float                   ScrollTop    = 0;
}