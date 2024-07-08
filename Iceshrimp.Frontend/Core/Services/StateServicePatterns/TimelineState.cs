using System.Diagnostics.CodeAnalysis;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Core.Services.StateServicePatterns;

internal class Timeline(MessageService messageService)
{
	private MessageService                    MessageService { get; set; } = messageService;
	private          Dictionary<string, TimelineState> States         { get; }      = new();

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
	private MessageService     MessageService { get; set; }
	public required  string?            MaxId;
	public required  string?            MinId;
	public required  List<NoteResponse> Timeline;

	[SetsRequiredMembers]
	public TimelineState(List<NoteResponse> timeline, string? maxId, string? minId, MessageService messageService)
	{
		MaxId                      =  maxId;
		MinId                      =  minId;
		Timeline                   =  timeline;
		MessageService             =  messageService;
		MessageService.AnyNoteChanged += OnNoteChanged;

	}

	private void OnNoteChanged(object? _, NoteResponse note)
	{
		var i = Timeline.FindIndex(p => p.Id == note.Id);
		if (i >= 0)
		{
			Timeline[i].Liked = note.Liked;
		}
	}

	public void Dispose()
	{
		MessageService.AnyNoteChanged -= OnNoteChanged;
	}
}