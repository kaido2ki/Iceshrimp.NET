using Iceshrimp.Frontend.Core.Services;
using Iceshrimp.Shared.HubSchemas;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.Components;

namespace Iceshrimp.Frontend.Pages;

public partial class Streaming : IAsyncDisposable
{
	[Inject] private StreamingService StreamingService { get; init; } = null!;
	private readonly List<string>     _messages = [];

	protected override async Task OnInitializedAsync()
	{
		StreamingService.Notification  += OnNotification;
		StreamingService.NotePublished += OnNotePublished;
		StreamingService.NoteUpdated   += OnNoteUpdated;

		await StreamingService.Connect();
	}

	private async void OnNotification(object? _, NotificationResponse notification)
	{
		_messages.Add($"Notification: {notification.Id} ({notification.Type})");
		await InvokeAsync(StateHasChanged);
	}

	private async void OnNotePublished(object? _, (StreamingTimeline timeline, NoteResponse note) data)
	{
		_messages.Add($"Note: {data.note.Id} ({data.timeline.ToString()})");
		await InvokeAsync(StateHasChanged);
	}

	private async void OnNoteUpdated(object? _, (StreamingTimeline timeline, NoteResponse note) data)
	{
		_messages.Add($"Note updated: {data.note.Id} ({data.timeline.ToString()})");
		await InvokeAsync(StateHasChanged);
	}

	public async ValueTask DisposeAsync()
	{
		StreamingService.Notification  -= OnNotification;
		StreamingService.NotePublished -= OnNotePublished;
		StreamingService.NoteUpdated   -= OnNoteUpdated;

		await StreamingService.DisposeAsync();
		GC.SuppressFinalize(this);
	}
}