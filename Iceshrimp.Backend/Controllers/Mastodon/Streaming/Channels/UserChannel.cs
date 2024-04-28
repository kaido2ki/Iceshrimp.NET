using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class UserChannel(WebSocketConnection connection, bool notificationsOnly) : IChannel
{
	private readonly ILogger<UserChannel> _logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<UserChannel>>();

	public string       Name         => notificationsOnly ? "user:notification" : "user";
	public List<string> Scopes       => ["read:statuses", "read:notifications"];
	public bool         IsSubscribed { get; private set; }

	public async Task Subscribe(StreamingRequestMessage _)
	{
		if (IsSubscribed) return;
		IsSubscribed = true;

		await using var scope = connection.ScopeFactory.CreateAsyncScope();
		await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

		if (!notificationsOnly)
		{
			connection.EventService.NotePublished += OnNotePublished;
			connection.EventService.NoteUpdated   += OnNoteUpdated;
			connection.EventService.NoteDeleted   += OnNoteDeleted;
		}

		connection.EventService.Notification += OnNotification;
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
		if (!notificationsOnly)
		{
			connection.EventService.NotePublished -= OnNotePublished;
			connection.EventService.NoteUpdated   -= OnNoteUpdated;
			connection.EventService.NoteDeleted   -= OnNoteDeleted;
		}

		connection.EventService.Notification -= OnNotification;
	}

	private NoteWithVisibilities? IsApplicable(Note note)
	{
		if (!IsApplicableBool(note)) return null;
		var res = EnforceRenoteReplyVisibility(note);
		return res is not { Note.IsPureRenote: true, Renote: null } ? res : null;
	}

	private bool IsApplicableBool(Note note) =>
		connection.Following.Prepend(connection.Token.User.Id).Contains(note.UserId);

	private bool IsApplicable(Notification notification) => notification.NotifieeId == connection.Token.User.Id;

	private bool IsFiltered(Note note) => connection.IsFiltered(note.User) ||
	                                      (note.Renote?.User != null && connection.IsFiltered(note.Renote.User)) ||
	                                      note.Renote?.Renote?.User != null &&
	                                      connection.IsFiltered(note.Renote.Renote.User);

	private bool IsFiltered(Notification notification) =>
		(notification.Notifier != null && connection.IsFiltered(notification.Notifier)) ||
		(notification.Note != null && IsFiltered(notification.Note));

	private NoteWithVisibilities EnforceRenoteReplyVisibility(Note note)
	{
		var wrapped = new NoteWithVisibilities(note);
		if (!wrapped.Renote?.IsVisibleFor(connection.Token.User, connection.Following) ?? false)
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

	private async void OnNotePublished(object? _, Note note)
	{
		try
		{
			var wrapped = IsApplicable(note);
			if (wrapped == null) return;
			if (IsFiltered(note)) return;
			if (note.CreatedAt < DateTime.UtcNow - TimeSpan.FromMinutes(5)) return;
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
			_logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
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

	private async void OnNotification(object? _, Notification notification)
	{
		try
		{
			if (!IsApplicable(notification)) return;
			if (IsFiltered(notification)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer = scope.ServiceProvider.GetRequiredService<NotificationRenderer>();

			NotificationEntity rendered;
			try
			{
				rendered = await renderer.RenderAsync(notification, connection.Token.User);
			}
			catch (GracefulException)
			{
				// Unsupported notification type
				return;
			}

			var message = new StreamingUpdateMessage
			{
				Stream  = [Name],
				Event   = "notification",
				Payload = JsonSerializer.Serialize(rendered)
			};
			await connection.SendMessageAsync(JsonSerializer.Serialize(message));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler OnNotification threw exception: {e}", e);
		}
	}
}