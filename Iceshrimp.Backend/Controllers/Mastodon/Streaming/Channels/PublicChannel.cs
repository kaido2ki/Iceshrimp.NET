using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Core.Database.Tables;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class PublicChannel(
	WebSocketConnection connection,
	string name,
	bool local,
	bool remote,
	bool onlyMedia
) : IChannel
{
	public string       Name         => name;
	public List<string> Scopes       => ["read:statuses"];
	public bool         IsSubscribed { get; private set; }

	public Task Subscribe(StreamingRequestMessage _)
	{
		if (IsSubscribed) return Task.CompletedTask;
		IsSubscribed = true;

		connection.EventService.NotePublished += OnNotePublished;
		connection.EventService.NoteUpdated   += OnNoteUpdated;
		connection.EventService.NoteDeleted   += OnNoteDeleted;
		return Task.CompletedTask;
	}

	public Task Unsubscribe(StreamingRequestMessage _)
	{
		if (!IsSubscribed) return Task.CompletedTask;
		IsSubscribed = false;
		Dispose();
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		connection.EventService.NotePublished -= OnNotePublished;
		connection.EventService.NoteUpdated   -= OnNoteUpdated;
		connection.EventService.NoteDeleted   -= OnNoteDeleted;
	}

	private bool IsApplicable(Note note)
	{
		if (note.Visibility != Note.NoteVisibility.Public) return false;
		if (!local && note.UserHost == null) return false;
		if (!remote && note.UserHost != null) return false;
		if (onlyMedia && note.FileIds.Count == 0) return false;

		return true;
	}

	private async void OnNotePublished(object? _, Note note)
	{
		if (!IsApplicable(note)) return;
		var provider = connection.ScopeFactory.CreateScope().ServiceProvider;
		var renderer = provider.GetRequiredService<NoteRenderer>();
		var rendered = await renderer.RenderAsync(note, connection.Token.User);
		var message = new StreamingUpdateMessage
		{
			Stream = [Name], Event = "update", Payload = JsonSerializer.Serialize(rendered)
		};
		await connection.SendMessageAsync(JsonSerializer.Serialize(message));
	}

	private async void OnNoteUpdated(object? _, Note note)
	{
		if (!IsApplicable(note)) return;
		var provider = connection.ScopeFactory.CreateScope().ServiceProvider;
		var renderer = provider.GetRequiredService<NoteRenderer>();
		var rendered = await renderer.RenderAsync(note, connection.Token.User);
		var message = new StreamingUpdateMessage
		{
			Stream = [Name], Event = "status.update", Payload = JsonSerializer.Serialize(rendered)
		};
		await connection.SendMessageAsync(JsonSerializer.Serialize(message));
	}

	private async void OnNoteDeleted(object? _, Note note)
	{
		if (!IsApplicable(note)) return;
		var message = new StreamingUpdateMessage { Stream = [Name], Event = "delete", Payload = note.Id };
		await connection.SendMessageAsync(JsonSerializer.Serialize(message));
	}
}