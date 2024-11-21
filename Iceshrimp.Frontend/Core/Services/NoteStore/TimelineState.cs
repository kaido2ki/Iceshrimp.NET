using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.NoteStore;

internal class TimelineState
{
	public SortedList<string, NoteResponse> Timeline { get; } = new(Comparer<string>.Create((x, y) => y.CompareTo(x)));
	public string?                          MaxId    { get; set; }
	public string?                          MinId    { get; set; }
}