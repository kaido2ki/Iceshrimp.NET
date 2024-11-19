using System.Collections.Concurrent;
using Iceshrimp.Backend.Controllers.Web.Renderers;
using Iceshrimp.Backend.Core.Database.Tables;
using Iceshrimp.Backend.Core.Extensions;
using Iceshrimp.Backend.SignalR;
using Iceshrimp.Backend.SignalR.Helpers;
using Iceshrimp.Shared.Schemas.SignalR;
using Iceshrimp.Shared.Schemas.Web;
using Microsoft.AspNetCore.SignalR;

namespace Iceshrimp.Backend.Core.Services;

public sealed class StreamingService : ISingletonService
{
	private readonly ConcurrentDictionary<string, StreamingConnectionAggregate> _connections = [];
	private readonly EventService                                               _eventSvc;
	private readonly IHubContext<StreamingHub, IStreamingHubClient>             _hub;
	private readonly ILogger<StreamingService>                                  _logger;
	private readonly IServiceScopeFactory                                       _scopeFactory;

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

	public event EventHandler<(Note note, Lazy<Task<NoteResponse>> rendered)>? NotePublished;
	public event EventHandler<(Note note, Lazy<Task<NoteResponse>> rendered)>? NoteUpdated;

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

	public Task SubscribeAsync(string userId, string connectionId, StreamingTimeline timeline)
	{
		_connections.TryGetValue(userId, out var conn);
		conn?.Subscribe(connectionId, timeline);
		return Task.CompletedTask;
	}

	public Task UnsubscribeAsync(string userId, string connectionId, StreamingTimeline timeline)
	{
		_connections.TryGetValue(userId, out var conn);
		conn?.Unsubscribe(connectionId, timeline);
		return Task.CompletedTask;
	}

	private Lazy<Task<NoteResponse>> Render(Note note)
	{
		return new Lazy<Task<NoteResponse>>(async () =>
		{
			await using var scope    = _scopeFactory.CreateAsyncScope();
			var             renderer = scope.ServiceProvider.GetRequiredService<NoteRenderer>();
			return await renderer.RenderOne(note, null);
		});
	}

	private void OnNotePublished(object? _, Note note)
	{
		try
		{
			NotePublished?.Invoke(this, (note, Render(note)));
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
			NoteUpdated?.Invoke(this, (note, Render(note)));
		}
		catch (Exception e)
		{
			_logger.LogError("Event handler {name} threw exception: {e}", nameof(OnNoteUpdated), e);
		}
	}
}