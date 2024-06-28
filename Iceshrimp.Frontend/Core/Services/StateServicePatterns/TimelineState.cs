using Iceshrimp.Shared.Schemas;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

public class Timeline
{
	private Dictionary<string, TimelineState> States { get; } = new();

	public void SetState(string id, TimelineState state)
	{
		States[id] = state;
	}

	public TimelineState GetState(string id)
	{
		States.TryGetValue(id, out var state);
		return state ?? throw new ArgumentException($"Requested state '{id}' does not exist.");
	}
}

public class TimelineState
{
	public required string?            MaxId;
	public required string?            MinId;
	public required List<NoteResponse> Timeline = [];
}