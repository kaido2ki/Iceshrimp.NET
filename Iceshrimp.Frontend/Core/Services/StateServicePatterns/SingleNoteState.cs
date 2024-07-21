namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

public class SingleNote
{
	private Dictionary<string, SingleNoteState> States { get; set; } = new();
	
	public void SetState(string id, SingleNoteState state)
	{
		States[id] = state;
	}
	
	public SingleNoteState? GetState(string id)
	{
		States.TryGetValue(id, out var state);
		return state;
	}

}

public class SingleNoteState
{
	public float ScrollTop = 0;
}