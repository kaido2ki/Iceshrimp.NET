using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class ListChannel(WebSocketConnection connection) : IChannel
{
	private readonly WriteLockingHashSet<string> _lists = [];

	private readonly ILogger<ListChannel> _logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<ListChannel>>();

	private readonly ConcurrentDictionary<string, WriteLockingList<string>> _members           = [];
	private          IEnumerable<string>                                    _applicableUserIds = [];

	public string       Name         => "list";
	public List<string> Scopes       => ["read:statuses"];
	public bool         IsSubscribed => _lists.Count != 0;
	public bool         IsAggregate  => true;

	public async Task SubscribeAsync(StreamingRequestMessage msg)
	{
		if (msg.List == null)
		{
			await connection.CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		if (!IsSubscribed)
		{
			connection.EventService.NotePublished      += OnNotePublished;
			connection.EventService.NoteUpdated        += OnNoteUpdated;
			connection.EventService.NoteDeleted        += OnNoteDeleted;
			connection.EventService.ListMembersUpdated += OnListMembersUpdated;
		}

		if (_lists.AddIfMissing(msg.List))
		{
			await using var scope = connection.GetAsyncServiceScope();

			var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
			var list = await db.UserLists.FirstOrDefaultAsync(p => p.UserId == connection.Token.User.Id &&
			                                                       p.Id == msg.List);
			if (list == null)
			{
				await connection.CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
				return;
			}

			var members = await db.UserListMembers.Where(p => p.UserList == list).Select(p => p.UserId).ToListAsync();
			_members.AddOrUpdate(list.Id, _ => new WriteLockingList<string>(members), (_, existing) => existing);
			_applicableUserIds = _members.Values.SelectMany(p => p).Distinct();
		}
	}

	public async Task UnsubscribeAsync(StreamingRequestMessage msg)
	{
		if (msg.List == null)
		{
			await connection.CloseAsync(WebSocketCloseStatus.InvalidPayloadData);
			return;
		}

		_lists.RemoveWhere(p => p == msg.List);
		_members.TryRemove(msg.List, out _);
		_applicableUserIds = _members.Values.SelectMany(p => p).Distinct();

		if (!IsSubscribed) Dispose();
	}

	public void Dispose()
	{
		connection.EventService.NotePublished      -= OnNotePublished;
		connection.EventService.NoteUpdated        -= OnNoteUpdated;
		connection.EventService.NoteDeleted        -= OnNoteDeleted;
		connection.EventService.ListMembersUpdated -= OnListMembersUpdated;
	}

	private NoteWithVisibilities? IsApplicable(Note note)
	{
		if (!IsApplicableBool(note)) return null;
		var res = EnforceRenoteReplyVisibility(note);
		return res is not { Note.IsPureRenote: true, Renote: null } ? res : null;
	}

	private bool IsApplicableBool(Note note) =>
		_applicableUserIds.Contains(note.UserId) && note.IsVisibleFor(connection.Token.User, connection.Following);

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
		IEnumerable<string> lists, string eventType, string payload
	) => lists.Select(list => new StreamingUpdateMessage
	{
		Stream  = [Name, list],
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
			await using var scope = connection.GetAsyncServiceScope();
			if (await connection.IsMutedThreadAsync(note, scope)) return;

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var data         = new NoteRenderer.NoteRendererDto { Filters = connection.Filters.ToList() };
			var intermediate = await renderer.RenderAsync(note, connection.Token.User, data: data);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);

			var lists    = _members.Where(p => p.Value.Contains(note.UserId)).Select(p => p.Key);
			var messages = RenderMessage(lists, "update", JsonSerializer.Serialize(rendered));
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
			await using var scope = connection.GetAsyncServiceScope();

			var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var data         = new NoteRenderer.NoteRendererDto { Filters = connection.Filters.ToList() };
			var intermediate = await renderer.RenderAsync(note, connection.Token.User, data: data);
			var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);

			var lists    = _members.Where(p => p.Value.Contains(note.UserId)).Select(p => p.Key);
			var messages = RenderMessage(lists, "status.update", JsonSerializer.Serialize(rendered));
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

			var lists    = _members.Where(p => p.Value.Contains(note.UserId)).Select(p => p.Key);
			var messages = RenderMessage(lists, "status.update", note.Id);
			foreach (var message in messages)
				await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteDeleted threw exception: {e}", e);
		}
	}

	private async void OnListMembersUpdated(object? _, UserList list)
	{
		try
		{
			if (list.UserId != connection.Token.User.Id) return;
			if (!_lists.Contains(list.Id)) return;

			await using var scope = connection.GetAsyncServiceScope();

			var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
			var members = await db.UserListMembers.Where(p => p.UserListId == list.Id)
			                      .Select(p => p.UserId)
			                      .ToListAsync();

			var wlMembers = new WriteLockingList<string>(members);

			_members.AddOrUpdate(list.Id, _ => wlMembers, (_, _) => wlMembers);
			_applicableUserIds = _members.Values.SelectMany(p => p).Distinct();
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnListUpdated threw exception: {e}", e);
		}
	}

	private class NoteWithVisibilities(Note note)
	{
		public readonly Note  Note   = note;
		public          Note? Renote = note.Renote;
	}
}