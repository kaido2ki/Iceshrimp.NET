using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class DirectChannel(WebSocketConnection connection) : IChannel
{
	private readonly ILogger<DirectChannel> _logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<DirectChannel>>();

	public string       Name         => "direct";
	public List<string> Scopes       => ["read:statuses"];
	public bool         IsSubscribed { get; private set; }
	public bool         IsAggregate  => false;

	public async Task Subscribe(StreamingRequestMessage _)
	{
		if (IsSubscribed) return;
		IsSubscribed = true;

		await using var scope = connection.ScopeFactory.CreateAsyncScope();
		await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

		connection.EventService.NotePublished += OnNotePublished;
		connection.EventService.NoteUpdated   += OnNoteUpdated;
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
	}

	private NoteWithVisibilities? IsApplicable(Note note)
	{
		if (!IsApplicableBool(note)) return null;
		var res = EnforceRenoteReplyVisibility(note);
		return res is not { Note.IsPureRenote: true, Renote: null } ? res : null;
	}

	private bool IsApplicableBool(Note note) =>
		note.Visibility == Note.NoteVisibility.Specified && note.VisibleUserIds.Contains(connection.Token.User.Id);

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

	private async Task<ConversationEntity> RenderConversation(
		Note note, NoteWithVisibilities wrapped, AsyncServiceScope scope
	)
	{
		var db           = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
		var renderer     = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
		var userRenderer = scope.ServiceProvider.GetRequiredService<UserRenderer>();
		var intermediate = await renderer.RenderAsync(note, connection.Token.User);
		var rendered     = EnforceRenoteReplyVisibility(intermediate, wrapped);

		var users = await db.Users.IncludeCommonProperties()
		                    .Where(p => note.VisibleUserIds.Contains(p.Id))
		                    .ToListAsync();
		var accounts = await userRenderer.RenderManyAsync(users, connection.Token.User);

		return new ConversationEntity
		{
			Accounts   = accounts.ToList(),
			Id         = note.ThreadId,
			LastStatus = rendered,
			Unread     = true
		};
	}

	private async void OnNotePublished(object? _, Note note)
	{
		try
		{
			var wrapped = IsApplicable(note);
			if (wrapped == null) return;
			if (connection.IsFiltered(note)) return;
			if (note.CreatedAt < DateTime.UtcNow - TimeSpan.FromMinutes(5)) return;

			await using var scope = connection.ScopeFactory.CreateAsyncScope();
			if (await connection.IsMutedThread(note, scope)) return;

			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "conversation",
				Payload = JsonSerializer.Serialize(await RenderConversation(note, wrapped, scope))
			};

			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
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
			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "conversation",
				Payload = JsonSerializer.Serialize(await RenderConversation(note, wrapped, scope))
			};

			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
		}
	}

	private class NoteWithVisibilities(Note note)
	{
		public readonly Note  Note   = note;
		public          Note? Renote = note.Renote;
	}
}