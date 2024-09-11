using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class Search
{
	private SearchState? State { get; set; }

	public SearchState? GetState()
	{
		return State;
	}

	public void SetState(SearchState newState)
	{
		State = newState;
	}
}

internal class SearchState
{
	public required List<NoteResponse> SearchResults;
	public required float              ScrollTop = 0;
	public required string             SearchString;
}