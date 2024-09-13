using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Shared.Schemas.Web;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class Timeline(MessageService messageService)
{
	private MessageService                    MessageService { get; set; } = messageService;
	private Dictionary<string, TimelineState> States         { get; }      = new();

	public void SetState(string id, TimelineState state)
	{
		States[id] = state;
	}

	public TimelineState GetState(string id)
	{
		States.TryGetValue(id, out var state);
		if (state != null) return state;
		else
		{
			States[id] = new TimelineState([], null, null, MessageService);
			return States[id];
		}
	}
}

internal class TimelineState : IDisposable
{
	private         MessageService     MessageService { get; set; }
	public required string?            MaxId;
	public required string?            MinId;
	public required List<NoteResponse> Timeline;

	[SetsRequiredMembers]
	internal TimelineState(List<NoteResponse> timeline, string? maxId, string? minId, MessageService messageService)
	{
		MaxId                         =  maxId;
		MinId                         =  minId;
		Timeline                      =  timeline;
		MessageService                =  messageService;
		MessageService.AnyNoteChanged += OnNoteChanged;
		MessageService.AnyNoteDeleted += OnNoteDeleted;
	}

	private void OnNoteChanged(object? _, NoteResponse note)
	{
		var i = Timeline.FindIndex(p => p.Id == note.Id);
		if (i >= 0)
		{
			Timeline[i] = note;
		}
	}

	private void OnNoteDeleted(object? _, NoteResponse note)
	{
		var i = Timeline.FindIndex(p => p.Id == note.Id);
		if (Timeline.Count <= 1)
		{
			Timeline.RemoveAt(i);
			return;
		}

		if (i == 0) MaxId                  = Timeline[1].Id;
		if (i == Timeline.Count - 1) MinId = Timeline[^2].Id;
		Timeline.RemoveAt(i);
	}

	public void Dispose()
	{
		MessageService.AnyNoteChanged -= OnNoteChanged;
		MessageService.AnyNoteDeleted -= OnNoteDeleted;
	}
}