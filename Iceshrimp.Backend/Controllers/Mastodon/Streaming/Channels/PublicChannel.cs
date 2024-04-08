using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
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
	private readonly ILogger<PublicChannel> _logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<PublicChannel>>();

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

	private NoteWithVisibilities? IsApplicable(Note note)
	{
		if (!IsApplicableBool(note)) return null;
		var res = EnforceRenoteReplyVisibility(note);
		return res is not { Note.IsPureRenote: true, Renote: null } ? null : res;
	}

	private bool IsApplicableBool(Note note)
	{
		if (note.Visibility != Note.NoteVisibility.Public) return false;
		if (!local && note.UserHost == null) return false;
		if (!remote && note.UserHost != null) return false;
		return !onlyMedia || note.FileIds.Count != 0;
	}

	private NoteWithVisibilities EnforceRenoteReplyVisibility(Note note)
	{
		var wrapped = new NoteWithVisibilities(note);
		if (wrapped.Renote?.IsVisibleFor(connection.Token.User, connection.Following) ?? false)
			wrapped.Renote = null;

		return wrapped;
	}

	private class NoteWithVisibilities(Note note)
	{
		public readonly Note  Note   = note;
		public          Note? Renote = note.Renote;
	}

	private static StatusEntity EnforceRenoteReplyVisibility(StatusEntity rendered, NoteWithVisibilities note)
	{
		var renote = note.Renote == null && rendered.Renote != null;
		if (!renote) return rendered;

		rendered = (StatusEntity)rendered.Clone();
		if (renote) rendered.Renote = null;
		return rendered;
	}

	private bool IsFiltered(Note note) => connection.IsFiltered(note.User) ||
	                                      (note.Renote?.User != null && connection.IsFiltered(note.Renote.User)) ||
	                                      note.Renote?.Renote?.User != null &&
	                                      connection.IsFiltered(note.Renote.Renote.User);

	private async void OnNotePublished(object? _, Note note)
	{
		try
		{
			var wrapped = IsApplicable(note);
			if (wrapped == null) return;
			if (IsFiltered(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var intermediate = await renderer.RenderAsync(note, connection.Token.User);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);
			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "update",
				Payload = JsonSerializer.Serialize(rendered)
			};
			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNotePublished threw exception: {e}", e);
		}
	}

	private async void OnNoteUpdated(object? _, Note note)
	{
		try
		{
			var wrapped = IsApplicable(note);
			if (wrapped == null) return;
			if (IsFiltered(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var intermediate = await renderer.RenderAsync(note, connection.Token.User);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);
			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "status.update",
				Payload = JsonSerializer.Serialize(rendered)
			};
			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
		}
	}

	private async void OnNoteDeleted(object? _, Note note)
	{
		try
		{
			if (!IsApplicableBool(note)) return;
			if (IsFiltered(note)) return;
			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "delete",
				Payload = note.Id
			};
			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteDeleted threw exception: {e}", e);
		}
	}
}