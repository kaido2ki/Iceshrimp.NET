using System.Net.WebSockets;
using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.Core.Helpers;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class HashtagChannel(WebSocketConnection connection, bool local) : IChannel
{
	private readonly ILogger<HashtagChannel> _logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<HashtagChannel>>();

	private readonly WriteLockingHashSet<string> _tags = [];

	public string       Name         => local ? "hashtag:local" : "hashtag";
	public List<string> Scopes       => ["read:statuses"];
	public bool         IsSubscribed => _tags.Count != 0;
	public bool         IsAggregate  => true;

	public async Task Subscribe(StreamingRequestMessage msg)
	{
		if (msg.Tag == null)
		{
			await connection.CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		if (!IsSubscribed)
		{
			connection.EventService.NotePublished += OnNotePublished;
			connection.EventService.NoteUpdated   += OnNoteUpdated;
			connection.EventService.NoteDeleted   += OnNoteDeleted;
		}

		_tags.AddIfMissing(msg.Tag);
	}

	public async Task Unsubscribe(StreamingRequestMessage msg)
	{
		if (msg.Tag == null)
		{
			await connection.CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		_tags.RemoveWhere(p => p == msg.Tag);

		if (!IsSubscribed) Dispose();
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
		return res is not { Note.IsPureRenote: true, Renote: null } ? res : null;
	}

	private bool IsApplicableBool(Note note) =>
		(!local || note.User.IsLocalUser) &&
		note.Tags.Intersects(_tags) &&
		note.IsVisibleFor(connection.Token.User, connection.Following);

	private NoteWithVisibilities EnforceRenoteReplyVisibility(Note note)
	{
		var wrapped = new NoteWithVisibilities(note);
		if (!wrapped.Renote?.IsVisibleFor(connection.Token.User, connection.Following) ?? false)
			wrapped.Renote = null;

		return wrapped;
	}

	private static StatusEntity EnforceRenoteReplyVisibility(StatusEntity rendered, NoteWithVisibilities note)
	{
		var renote = note.Renote == null && rendered.Renote != null;
		if (!renote) return rendered;

		rendered = (StatusEntity)rendered.Clone();
		if (renote) rendered.Renote = null;
		return rendered;
	}

	private IEnumerable<StreamingUpdateMessage> RenderMessage(
		IEnumerable<string> tags, string eventType, string payload
	) => tags.Select(tag => new StreamingUpdateMessage
	{
		Stream  = [Name, tag],
		Event   = eventType,
		Payload = payload
	});

	private async void OnNotePublished(object? _, Note note)
	{
		try
		{
			var wrapped = IsApplicable(note);
			if (wrapped == null) return;
			if (connection.IsFiltered(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();
			if (await connection.IsMutedThread(note, scope)) return;

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var data         = new NoteRenderer.NoteRendererDto { Filters = connection.Filters.ToList() };
			var intermediate = await renderer.RenderAsync(note, connection.Token.User, data: data);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);

			var messages = RenderMessage(_tags.Intersect(note.Tags), "update", JsonSerializer.Serialize(rendered));
			foreach (var message in messages)
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
			if (connection.IsFiltered(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var data         = new NoteRenderer.NoteRendererDto { Filters = connection.Filters.ToList() };
			var intermediate = await renderer.RenderAsync(note, connection.Token.User, data: data);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);

			var messages = RenderMessage(_tags.Intersect(note.Tags), "status.update",
			                             JsonSerializer.Serialize(rendered));
			foreach (var message in messages)
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
			if (connection.IsFiltered(note)) return;

			var messages = RenderMessage(_tags.Intersect(note.Tags), "delete", note.Id);
			foreach (var message in messages)
				await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteDeleted threw exception: {e}", e);
		}
	}

	private class NoteWithVisibilities(Note note)
	{
		public readonly Note  Note   = note;
		public          Note? Renote = note.Renote;
	}
}