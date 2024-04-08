using System.Collections.Concurrent;
using AsyncKeyedLock;
using Iceshrimp.Backend.Controllers.Renderers;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Hubs;
using Iceshrimp.Backend.Hubs.Helpers;
using Iceshrimp.Shared.HubSchemas;
using Iceshrimp.Shared.Schemas;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.Core.Services;

public sealed class StreamingService
{
	private readonly ConcurrentDictionary<string, StreamingConnectionAggregate> _connections = [];
	private readonly IHubContext<StreamingHub, IStreamingHubClient>             _hub;
	private readonly EventService                                               _eventSvc;
	private readonly IServiceScopeFactory                                       _scopeFactory;
	private readonly ILogger<StreamingService>                                  _logger;

	private static readonly AsyncKeyedLocker<string> Locker = new(o =>
	{
		o.PoolSize        = 100;
		o.PoolInitialFill = 5;
	});

	public event EventHandler<(Note note, Func<Task<NoteResponse>> rendered)>? NotePublished;
	public event EventHandler<(Note note, Func<Task<NoteResponse>> rendered)>? NoteUpdated;

	public StreamingService(
		IHubContext<StreamingHub, IStreamingHubClient> hub,
		EventService eventSvc,
		IServiceScopeFactory scopeFactory,
		ILogger<StreamingService> logger
	)
	{
		_hub          = hub;
		_eventSvc     = eventSvc;
		_scopeFactory = scopeFactory;
		_logger       = logger;

		eventSvc.NotePublished += OnNotePublished;
		eventSvc.NoteUpdated   += OnNoteUpdated;
	}

	public void Connect(string userId, User user, string connectionId)
	{
		_connections
			.GetOrAdd(userId, _ => new StreamingConnectionAggregate(userId, user, _hub, _eventSvc, _scopeFactory, this))
			.Connect(connectionId);
	}

	public void Disconnect(string userId, string connectionId)
	{
		_connections.TryGetValue(userId, out var conn);
		if (conn == null) return;

		conn.Disconnect(connectionId);
		if (!conn.HasSubscribers && _connections.TryRemove(userId, out conn))
			conn.Dispose();
	}

	public void DisconnectAll(string userId)
	{
		_connections.TryRemove(userId, out var conn);
		conn?.Dispose();
	}

	public Task Subscribe(string userId, string connectionId, StreamingTimeline timeline)
	{
		_connections.TryGetValue(userId, out var conn);
		conn?.Subscribe(connectionId, timeline);
		return Task.CompletedTask;
	}

	public Task Unsubscribe(string userId, string connectionId, StreamingTimeline timeline)
	{
		_connections.TryGetValue(userId, out var conn);
		conn?.Unsubscribe(connectionId, timeline);
		return Task.CompletedTask;
	}

	private async Task<NoteResponse> Render(Note note)
	{
		NoteResponse? res = null;
		using (await Locker.LockAsync(note.Id))
		{
			if (res != null) return res;
			await using var scope = _scopeFactory.CreateAsyncScope();

			var renderer = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			res = await renderer.RenderOne(note, null);
		}

		return res ?? throw new Exception("NoteResponse was null after async lock");
	}

	private void OnNotePublished(object? _, Note note)
	{
		try
		{
			NotePublished?.Invoke(this, (note, () => Render(note)));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler {name} threw exception: {e}", nameof(OnNotePublished), e);
		}
	}

	private void OnNoteUpdated(object? _, Note note)
	{
		try
		{
			NoteUpdated?.Invoke(this, (note, () => Render(note)));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler {name} threw exception: {e}", nameof(OnNoteUpdated), e);
		}
	}
}