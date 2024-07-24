using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Iceshrimp.Backend.Core.Configuration;
using Iceshrimp.Backend.Core.Database;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Events;
using Iceshrimp.Backend.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Iceshrimp.Backend.Core.Services;

public class ClusteredEventService(
	IServiceScopeFactory scopeFactory,
	IOptions<Config.DatabaseSection> dbConfig,
	IOptions<Config.WorkerSection> workerConfig,
	ILogger<ClusteredEventService> logger
) : BackgroundService, IEventService
{
	private readonly NpgsqlDataSource _dataSource = DatabaseContext.GetDataSource(dbConfig.Value);

	public event EventHandler<Note>?            NotePublished;
	public event EventHandler<Note>?            NoteUpdated;
	public event EventHandler<Note>?            NoteDeleted;
	public event EventHandler<NoteInteraction>? NoteLiked;
	public event EventHandler<NoteInteraction>? NoteUnliked;
	public event EventHandler<NoteReaction>?    NoteReacted;
	public event EventHandler<NoteReaction>?    NoteUnreacted;
	public event EventHandler<UserInteraction>? UserFollowed;
	public event EventHandler<UserInteraction>? UserUnfollowed;
	public event EventHandler<UserInteraction>? UserBlocked;
	public event EventHandler<UserInteraction>? UserUnblocked;
	public event EventHandler<UserInteraction>? UserMuted;
	public event EventHandler<UserInteraction>? UserUnmuted;
	public event EventHandler<Notification>?    Notification;
	public event EventHandler<Filter>?          FilterAdded;
	public event EventHandler<Filter>?          FilterRemoved;
	public event EventHandler<Filter>?          FilterUpdated;
	public event EventHandler<UserList>?        ListMembersUpdated;

	public Task RaiseNotePublished(object? sender, Note note) =>
		EmitEvent(new NotePublishedEvent { Args = note });

	public Task RaiseNoteUpdated(object? sender, Note note) =>
		EmitEvent(new NoteUpdatedEvent { Args = note });

	public Task RaiseNoteDeleted(object? sender, Note note) =>
		EmitEvent(new NoteDeletedEvent { Args = note });

	public Task RaiseNotification(object? sender, Notification notification) =>
		EmitEvent(new NotificationEvent { Args = notification });

	public Task RaiseNotifications(object? sender, IEnumerable<Notification> notifications) =>
		notifications.Select(p => RaiseNotification(sender, p)).AwaitAllAsync();

	public Task RaiseNoteLiked(object? sender, Note note, User user) =>
		EmitEvent(new NoteLikedEvent { Args = new NoteInteraction { Note = note, User = user } });

	public Task RaiseNoteUnliked(object? sender, Note note, User user) =>
		EmitEvent(new NoteUnlikedEvent { Args = new NoteInteraction { Note = note, User = user } });

	public Task RaiseNoteReacted(object? sender, NoteReaction reaction) =>
		EmitEvent(new NoteReactedEvent { Args = reaction });

	public Task RaiseNoteUnreacted(object? sender, NoteReaction reaction) =>
		EmitEvent(new NoteUnreactedEvent { Args = reaction });

	public Task RaiseUserFollowed(object? sender, User actor, User obj) =>
		EmitEvent(new UserFollowedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseUserUnfollowed(object? sender, User actor, User obj) =>
		EmitEvent(new UserUnfollowedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseUserBlocked(object? sender, User actor, User obj) =>
		EmitEvent(new UserBlockedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseUserUnblocked(object? sender, User actor, User obj) =>
		EmitEvent(new UserUnblockedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseUserMuted(object? sender, User actor, User obj) =>
		EmitEvent(new UserMutedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseUserUnmuted(object? sender, User actor, User obj) =>
		EmitEvent(new UserUnmutedEvent { Args = new UserInteraction { Actor = actor, Object = obj } });

	public Task RaiseFilterAdded(object? sender, Filter filter) =>
		EmitEvent(new FilterAddedEvent { Args = filter });

	public Task RaiseFilterRemoved(object? sender, Filter filter) =>
		EmitEvent(new FilterRemovedEvent { Args = filter });

	public Task RaiseFilterUpdated(object? sender, Filter filter) =>
		EmitEvent(new FilterUpdatedEvent { Args = filter });

	public Task RaiseListMembersUpdated(object? sender, UserList list) =>
		EmitEvent(new ListMembersUpdatedEvent { Args = list });

	[JsonDerivedType(typeof(NotePublishedEvent), "notePublished")]
	[JsonDerivedType(typeof(NoteUpdatedEvent), "noteUpdated")]
	[JsonDerivedType(typeof(NoteDeletedEvent), "noteDeleted")]
	[JsonDerivedType(typeof(NoteLikedEvent), "noteLiked")]
	[JsonDerivedType(typeof(NoteUnlikedEvent), "noteUnliked")]
	[JsonDerivedType(typeof(NotificationEvent), "notification")]
	private interface IClusterEvent
	{
		public string Payload { get; }
	}

	private class NotePublishedEvent : ClusterEvent<Note>;

	private class NoteUpdatedEvent : ClusterEvent<Note>;

	private class NoteDeletedEvent : ClusterEvent<Note>;

	private class NoteLikedEvent : ClusterEvent<NoteInteraction>;

	private class NoteUnlikedEvent : ClusterEvent<NoteInteraction>;

	private class NoteReactedEvent : ClusterEvent<NoteReaction>;

	private class NoteUnreactedEvent : ClusterEvent<NoteReaction>;

	private class UserFollowedEvent : ClusterEvent<UserInteraction>;

	private class UserUnfollowedEvent : ClusterEvent<UserInteraction>;

	private class UserBlockedEvent : ClusterEvent<UserInteraction>;

	private class UserUnblockedEvent : ClusterEvent<UserInteraction>;

	private class UserMutedEvent : ClusterEvent<UserInteraction>;

	private class UserUnmutedEvent : ClusterEvent<UserInteraction>;

	private class NotificationEvent : ClusterEvent<Notification>;

	private class FilterAddedEvent : ClusterEvent<Filter>;

	private class FilterRemovedEvent : ClusterEvent<Filter>;

	private class FilterUpdatedEvent : ClusterEvent<Filter>;

	private class ListMembersUpdatedEvent : ClusterEvent<UserList>;

	private void HandleEvent(string payload)
	{
		logger.LogInformation("Handling event: {payload}", payload);

		var deserialized = JsonSerializer.Deserialize<IClusterEvent>(payload) ??
		                   throw new Exception("Failed to deserialize cluster event");

		switch (deserialized)
		{
			case NotePublishedEvent e:
				NotePublished?.Invoke(this, e.Args);
				break;
			case NoteUpdatedEvent e:
				NoteUpdated?.Invoke(this, e.Args);
				break;
			case NoteDeletedEvent e:
				NoteDeleted?.Invoke(this, e.Args);
				break;
			case NoteLikedEvent e:
				NoteLiked?.Invoke(this, e.Args);
				break;
			case NoteUnlikedEvent e:
				NoteUnliked?.Invoke(this, e.Args);
				break;
			case NoteReactedEvent e:
				NoteReacted?.Invoke(this, e.Args);
				break;
			case NoteUnreactedEvent e:
				NoteUnreacted?.Invoke(this, e.Args);
				break;
			case UserFollowedEvent e:
				UserFollowed?.Invoke(this, e.Args);
				break;
			case UserUnfollowedEvent e:
				UserUnfollowed?.Invoke(this, e.Args);
				break;
			case UserBlockedEvent e:
				UserBlocked?.Invoke(this, e.Args);
				break;
			case UserUnblockedEvent e:
				UserUnblocked?.Invoke(this, e.Args);
				break;
			case UserMutedEvent e:
				UserMuted?.Invoke(this, e.Args);
				break;
			case UserUnmutedEvent e:
				UserUnmuted?.Invoke(this, e.Args);
				break;
			case NotificationEvent e:
				Notification?.Invoke(this, e.Args);
				break;
			case FilterAddedEvent e:
				FilterAdded?.Invoke(this, e.Args);
				break;
			case FilterRemovedEvent e:
				FilterRemoved?.Invoke(this, e.Args);
				break;
			case FilterUpdatedEvent e:
				FilterUpdated?.Invoke(this, e.Args);
				break;
			case ListMembersUpdatedEvent e:
				ListMembersUpdated?.Invoke(this, e.Args);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(payload), @"Unknown event type");
		}
	}

	private static readonly JsonSerializerOptions Options =
		new(JsonSerializerOptions.Default)
		{
			ReferenceHandler = ReferenceHandler.Preserve, IgnoreReadOnlyProperties = true
		};

	[SuppressMessage("ReSharper", "UnusedMember.Local")]
	private abstract class ClusterEvent<TEventArgs> : IClusterEvent
	{
		private readonly string?     _payload;
		private readonly TEventArgs? _args;

		public string Payload
		{
			get => _payload ?? throw new Exception("_payload was null");
			init
			{
				_payload = value;
				_args = JsonSerializer.Deserialize<TEventArgs>(value, Options) ??
				        throw new Exception("Failed to deserialize cluster event payload");
			}
		}

		public TEventArgs Args
		{
			get => _args ?? throw new Exception("_args was null");
			init
			{
				_args    = value;
				_payload = JsonSerializer.Serialize(value, Options);
			}
		}
	}

	protected override async Task ExecuteAsync(CancellationToken token)
	{
		if (workerConfig.Value.WorkerType is not Enums.WorkerType.QueueOnly)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					await using var conn = await GetNpgsqlConnection();

					conn.Notification += (_, args) =>
					{
						try
						{
							if (args.Channel is not "event") return;
							HandleEvent(args.Payload);
						}
						catch (Exception e)
						{
							logger.LogError("Failed to handle event: {error}", e);
						}
					};

					await using (var cmd = new NpgsqlCommand("LISTEN event", conn))
					{
						await cmd.ExecuteNonQueryAsync(token);
					}

					while (!token.IsCancellationRequested)
					{
						await conn.WaitAsync(token);
					}
				}
				catch
				{
					// ignored (logging this would spam logs on postgres restart)
				}
			}
		}
	}

	private async Task EmitEvent(IClusterEvent ev)
	{
		await using var scope = GetScope();
		await using var db    = GetDbContext(scope);

		var serialized = JsonSerializer.Serialize(ev, Options);
		logger.LogInformation("Emitting event: {serialized}", serialized);
		await db.Database.ExecuteSqlAsync($"SELECT pg_notify('event', {serialized});");
	}

	private async Task<NpgsqlConnection> GetNpgsqlConnection() =>
		await _dataSource.OpenConnectionAsync();

	private AsyncServiceScope GetScope() => scopeFactory.CreateAsyncScope();

	private static DatabaseContext GetDbContext(IServiceScope scope) =>
		scope.ServiceProvider.GetRequiredService<DatabaseContext>();
}