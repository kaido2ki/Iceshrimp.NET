using System.Text.Json;
using Iceshrimp.Backend.Controllers.Mastodon.Renderers;
using Iceshrimp.Backend.Controllers.Mastodon.Schemas.Entities;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Middleware;
using Microsoft.EntityFrameworkCore;

namespace Iceshrimp.Backend.Controllers.Mastodon.Streaming.Channels;

public class UserChannel(WebSocketConnection connection, bool notificationsOnly) : IChannel
{
	public readonly ILogger<UserChannel> Logger =
		connection.Scope.ServiceProvider.GetRequiredService<ILogger<UserChannel>>();

	private List<string> _followedUsers = [];
	public  string       Name         => notificationsOnly ? "user:notification" : "user";
	public  List<string> Scopes       => ["read:statuses", "read:notifications"];
	public  bool         IsSubscribed { get; private set; }

	public async Task Subscribe(StreamingRequestMessage _)
	{
		if (IsSubscribed) return;
		IsSubscribed = true;

		await using var scope = connection.ScopeFactory.CreateAsyncScope();
		await using var db    = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

		_followedUsers = await db.Users.Where(p => p == connection.Token.User)
		                         .SelectMany(p => p.Following)
		                         .Select(p => p.Id)
		                         .ToListAsync();

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

	private bool IsApplicable(Note note) => _followedUsers.Prepend(connection.Token.User.Id).Contains(note.UserId);
	private bool IsApplicable(Notification notification) => notification.NotifieeId == connection.Token.User.Id;

	private async void OnNotePublished(object? _, Note note)
	{
		try
		{
			if (!IsApplicable(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var rendered = await renderer.RenderAsync(note, connection.Token.User);
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
			Logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
		}
	}

	private async void OnNoteUpdated(object? _, Note note)
	{
		try
		{
			if (!IsApplicable(note)) return;
			await using var scope = connection.ScopeFactory.CreateAsyncScope();

			var renderer = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			var rendered = await renderer.RenderAsync(note, connection.Token.User);
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
			Logger.LogError("Event handler OnNoteUpdated threw exception: {e}", e);
		}
	}

	private async void OnNoteDeleted(object? _, Note note)
	{
		try
		{
			if (!IsApplicable(note)) return;
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
			Logger.LogError("Event handler OnNoteDeleted threw exception: {e}", e);
		}
	}

	private async void OnNotification(object? _, Notification notification)
	{
		try
		{
			if (!IsApplicable(notification)) return;
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
			Logger.LogError("Event handler OnNotification threw exception: {e}", e);
		}
	}
}