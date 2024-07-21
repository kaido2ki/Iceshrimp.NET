using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class VirtualScroller(MessageService messageService)
{
	private Dictionary<string, VirtualScrollerState> States { get; } = new();

	public VirtualScrollerState CreateStateObject()
	{
		return new VirtualScrollerState(messageService);
	}

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

internal class VirtualScrollerState : IDisposable
{
	internal VirtualScrollerState(MessageService messageService)
	{
		_messageService                =  messageService;
		_messageService.AnyNoteChanged += OnNoteChanged;
		_messageService.AnyNoteDeleted += OnNoteDeleted;
	}

	public  Dictionary<string, int> Height       = new();
	public  int                     PadBottom    = 0;
	public  int                     PadTop       = 0;
	public  List<NoteResponse>      RenderedList = [];
	public  float                   ScrollTop    = 0;
	private MessageService          _messageService;

	private void OnNoteChanged(object? _, NoteResponse note)
	{
		var i                       = RenderedList.FindIndex(p => p.Id == note.Id);
		if (i >= 0) RenderedList[i] = note;
	}

	private void OnNoteDeleted(object? _, NoteResponse note)
	{
		var i = RenderedList.FindIndex(p => p.Id == note.Id);
		if (i >= 0) RenderedList.RemoveAt(i);
	}

	public void Dispose()
	{
		_messageService.AnyNoteChanged -= OnNoteChanged;
		_messageService.AnyNoteDeleted -= OnNoteDeleted;
	}
}